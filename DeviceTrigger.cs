using Usb.Events;

public class DeviceTrigger
{
    public string? DeviceName { get; set; }
    public string? Product { get; set; }
    public string? ProductDescription { get; set; }
    public string? ProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Vendor { get; set; }
    public string? VendorDescription { get; set; }
    public string? VendorId { get; set; }

    public static DeviceTrigger FromUsbDevice(UsbDevice device)
    {
        return new DeviceTrigger
        {
            DeviceName = device.DeviceName,
            Product = device.Product,
            ProductDescription = device.ProductDescription,
            ProductId = device.ProductID,
            SerialNumber = device.SerialNumber,
            Vendor = device.Vendor,
            VendorDescription = device.VendorDescription,
            VendorId = device.VendorID
        };
    }

    public bool Matches(UsbDevice device)
    {
        // Matches if all stored properties match the current device's properties
        return string.Equals(DeviceName, device.DeviceName) &&
               string.Equals(Product, device.Product) &&
               string.Equals(ProductDescription, device.ProductDescription) &&
               string.Equals(ProductId, device.ProductID) &&
               string.Equals(SerialNumber, device.SerialNumber) &&
               string.Equals(Vendor, device.Vendor) &&
               string.Equals(VendorDescription, device.VendorDescription) &&
               string.Equals(VendorId, device.VendorID);
    }
}