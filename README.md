# YubiKill

A versatile cross-platform USB dead-man-switch for your PC.

Inspired by the [BusKill App](https://github.com/BusKill/buskill-app), which seems to be no longer maintained sadly.

## Overview

This is designed / intended to be used with a ["BusKill-Cable"](https://docs.buskill.in/buskill-app/en/stable/hardware_dev/bom.html), 
which you can make yourself for around 50$. There are "USB-A" and also "USB-C" variants. Essentially, you only need a
magnetic USB "break-away" adapter and a fitting cable with a device you can tether to yourself.

Hence, here is a "fresh" and clean CLI remake of BusKill, also intended to work for NON-Storage USB Devices, like:
`SmartCards`, `Keyboards`, `Mice`, `YubiKeys`, `USB-Hubs`, etc. and also work Cross-platform!

Generally everything "USB" with some kind of identifier-information should work.

In theory, we could also listen to a specific USB-Port via the "path" but that seems rather unreliable in comparison 
to a specific trigger device. Let me know if that's a use-case for you.

## Important for macOS

Currently, the nuget package `Jinjinov/Usb.Events` doesn't work fully on some macOS machines due to path limitations.

E.g.: if you have several nested USB Hubs (haha thanks Apple), the Path might be too long and crash the event listener.

Hence, I forked and fixed it here: https://github.com/Jinjinov/Usb.Events/pull/56 and am currently waiting for PR merge.

If that hasn't happened yet, and you want to use it on macOS, you have to check out my fork locally, add it as a Reference
to this "Solution" file and then add it as a Reference to the "Project" file, after removing the installed Nuget package.

Then you should be able to get a working build.

## Actions

There are currently three "Removed" actions to choose from:

1. Lock the screen
2. Logout User
3. Shutdown Device

All of them _should_ be working cross-platform, on Windows, Linux and macOs although I can currently **only test this on macOs**.

If you encounter any issues, feel free to open an Issue and/or PR.

## Usage

1. Run the Tool with `--configure`argument to set up the trigger devices (yes, multiple ones also work) and the action.
    - The tool will then create the `yubikill_config.json` file accordingly. See below for an example.
2. Run the tool without any arguments to start listening on triggers.

### Example Config

```json
{
  "TriggerDevices": [
    {
      "DeviceName": "YubiKey OTP\u002BFIDO\u002BCCID",
      "Product": "YubiKey OTP\u002BFIDO\u002BCCID",
      "ProductDescription": "YubiKey OTP\u002BFIDO\u002BCCID",
      "ProductId": "1234",
      "SerialNumber": "",
      "Vendor": "Yubico",
      "VendorDescription": "Yubico",
      "VendorId": "1234"
    }
  ],
  "Action": "Lock"
}
```

### Example Output

```
YubiKill Active. Monitoring 1 devices for removal...
Action on trigger: Lock
Press Enter to exit (or kill process).
init_notifier ok
Starting notifier

USB device FOUND: YubiKey OTP+FIDO+CCID
        Device path: [TRUNCATED]
        Device class name: IOUSBHostDevice
        Device vendor name: Yubico
        Vendor id: 1234
        Device product name: YubiKey OTP+FIDO+CCID
        Product id: 1234

USB device REMOVED: YubiKey OTP+FIDO+CCID
        Device path: [TRUNCATED]
        Device class name: IOUSBHostDevice
        Device vendor name: Yubico
        Vendor id: 1234
        Device product name: YubiKey OTP+FIDO+CCID
        Product id: 1234

TRIGGER DEVICE REMOVED: YubiKey OTP+FIDO+CCID
Locking Workstation...
```

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
