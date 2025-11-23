public class AppConfig
{
    public List<DeviceTrigger> TriggerDevices { get; set; } = [];
    
    public TriggerAction Action { get; set; } = TriggerAction.Lock;
}