using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Usb.Events;

const string configFileName = ".yubikill_config.json";

var fullConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), configFileName);

var configExists = File.Exists(fullConfigPath);
if (!configExists)
{
    Console.WriteLine($"Configuration file not found at: {fullConfigPath}.");
}

var hiddenArg = args.Contains("--hidden");

if (args.Contains("--configure") || !configExists)
{
    RunConfigurationMode(fullConfigPath);
}
else
{
    await RunMonitorMode(fullConfigPath, hiddenArg);
}

return;

static void RunConfigurationMode(string fullConfigPath)
{
    Console.WriteLine("--- YubiKill Setup Mode ---");

    // Initialize watcher to get current devices
    using IUsbEventWatcher usbEventWatcher =
        new UsbEventWatcher(startImmediately: true, addAlreadyPresentDevicesToList: true, usePnPEntity: true);

    // Give it a moment to populate
    Thread.Sleep(1000);

    var currentDevices = usbEventWatcher.UsbDeviceList;
    var selectedTriggers = new List<DeviceTrigger>();

    if (File.Exists(fullConfigPath))
    {
        try
        {
            var existingConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(fullConfigPath), AppJsonContext.Default.AppConfig);
            if (existingConfig?.TriggerDevices != null)
            {
                selectedTriggers = existingConfig.TriggerDevices;
                Console.WriteLine($"Loaded {selectedTriggers.Count} existing triggers.");
            }
        }
        catch
        {
            Console.WriteLine("Could not load existing config, starting fresh.");
        }
    }

    while (true)
    {
        Console.Clear();
        Console.WriteLine("Select devices to trigger on removal (Toggle by number):");
        Console.WriteLine("-------------------------------------------------------");

        for (var i = 0; i < currentDevices.Count; i++)
        {
            var device = currentDevices[i];
            // var tempTrigger = DeviceTrigger.FromUsbDevice(device);
            var isSelected = selectedTriggers.Any(t => t.Matches(device));

            var status = isSelected ? "[X]" : "[ ]";
            Console.WriteLine($"{i + 1}. {status} {device.DeviceName} ({device.Product} - {device.SerialNumber})");
        }

        Console.WriteLine("-------------------------------------------------------");
        Console.WriteLine("Enter number to toggle, 'R' to refresh list, or 'D' when done.");

        var input = Console.ReadLine()?.Trim().ToUpper();

        if (input == "D") break;
        if (input == "R") continue;

        if (int.TryParse(input, out var index) && index > 0 && index <= currentDevices.Count)
        {
            var deviceToToggle = currentDevices[index - 1];
            var existingMatch = selectedTriggers.FirstOrDefault(t => t.Matches(deviceToToggle));

            if (existingMatch != null)
            {
                selectedTriggers.Remove(existingMatch);
            }
            else
            {
                selectedTriggers.Add(DeviceTrigger.FromUsbDevice(deviceToToggle));
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("Select Action on Trigger:");
    Console.WriteLine("1. Lock Workstation");
    Console.WriteLine("2. Logout User");
    Console.WriteLine("3. Shutdown System");

    TriggerAction action;
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.KeyChar == '1')
        {
            action = TriggerAction.Lock;
            break;
        }
        if (key.KeyChar == '2')
        {
            action = TriggerAction.Logout;
            break;
        }
        if (key.KeyChar == '3')
        {
            action = TriggerAction.Shutdown;
            break;
        }
    }

    Console.WriteLine($"Selected Action: {action}");

    var config = new AppConfig
    {
        TriggerDevices = selectedTriggers,
        Action = action
    };

    File.WriteAllText(fullConfigPath, JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig));
    Console.WriteLine($"Configuration saved to {Path.GetFullPath(fullConfigPath)}");
    Console.WriteLine("Please restart the app now.");
}

static async Task RunMonitorMode(string fullConfigPath, bool hiddenArg)
{
    AppConfig? config;
    try
    {
        config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(fullConfigPath), AppJsonContext.Default.AppConfig);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading configuration: {ex.Message}");
        return;
    }

    if (config == null || config.TriggerDevices.Count == 0)
    {
        Console.WriteLine("No trigger devices configured.");
        return;
    }

    Console.WriteLine($"YubiKill Active. Monitoring {config.TriggerDevices.Count} devices for removal...");
    Console.WriteLine($"Action on trigger: {config.Action}");

    using IUsbEventWatcher usbEventWatcher =
        new UsbEventWatcher(startImmediately: true, addAlreadyPresentDevicesToList: true, usePnPEntity: true);

    usbEventWatcher.UsbDeviceRemoved += (_, device) =>
    {
        // Check if the removed device matches any of our configured triggers
        if (!config.TriggerDevices.Any(trigger => trigger.Matches(device))) return;
        Console.WriteLine($"TRIGGER DEVICE REMOVED: {device.DeviceName}");
        ExecuteAction(config.Action);
    };
    
    if (hiddenArg)
    {
        Console.WriteLine("YubiKill Active in Background...");
    
        // DO NOT USE Console.ReadLine() HERE.
        // It returns null immediately when no Terminal is attached.
    
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                // Use 'display notification' which is safe for Background Apps (LSUIElement)
                // unlike 'display dialog' which demands focus and causes Error 1002.
                Process.Start("osascript", "-e \"display notification \\\"Check Activity Monitor to stop.\\\" with title \\\"YubiKill Started\\\"\"");
            }
            catch
            {
                // Ignore notification failures
            }
        }

        // Use this instead to keep the app running forever:
        await Task.Delay(-1); 
    }
    else
    {
        Console.WriteLine("Press Enter to exit (or kill process).");
        Console.ReadLine(); // This is fine only when a Terminal IS present
    }
}

static void ExecuteAction(TriggerAction action)
{
    try
    {
        switch (action)
        {
            case TriggerAction.Shutdown:
            {
                Console.WriteLine("Initiating Shutdown...");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("shutdown", "/s /t 0 /f");
                }
                else // Linux and MacOS
                {
                    try
                    {
                        // mostly requires root, which we probably don't have.
                        Process.Start("halt", "-q");
                    }
                    catch
                    {
                        try
                        {
                            Process.Start("poweroff", "-f");
                        }
                        catch
                        {
                            Process.Start("shutdown", "-h now");
                        }
                    }
                }

                break;
            }
            case TriggerAction.Logout:
                Console.WriteLine("Logging out...");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("shutdown", "/l");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // “killall loginwindow” immediately terminates the user session.
                    // This does not require any special permissions, as loginwindow belongs to the user.
                    Process.Start("killall", "loginwindow");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        // clean way with systemd
                        Process.Start("loginctl", $"terminate-user {Environment.UserName}");
                    }
                    catch
                    {
                        // Fallback: kill everything from User (big bonk)
                        Process.Start("pkill", $"-KILL -u {Environment.UserName}");
                    }
                }
                break;
            case TriggerAction.Lock:
            {
                Console.WriteLine("Locking Workstation...");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    SACLockScreenImmediate();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Attempt common lock commands
                    try
                    {
                        Process.Start("xdg-screensaver", "lock");
                    }
                    catch
                    {
                        Process.Start("gnome-screensaver-command", "-l");
                    }
                }

                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to execute action: {ex.Message}");
        // In case of failure, we might want to force exit or try an alternative
    }
}

[DllImport("/System/Library/PrivateFrameworks/login.framework/Versions/Current/login")]
static extern int SACLockScreenImmediate();

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppJsonContext : JsonSerializerContext;
