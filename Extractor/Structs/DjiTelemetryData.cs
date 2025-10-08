namespace Extractor.Structs;

public class DjiTelemetryData
{
    public int FrameCount { get; set; }
    public int DiffTime { get; set; }
    public int ISO { get; set; }
    public string Shutter { get; set; }
    public double FNum { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RelativeAltitude { get; set; }
    public double AbsoluteAltitude { get; set; }
    
    public override string ToString()
    {
        return $"Frame: {FrameCount}, ISO: {ISO}, Shutter: {Shutter}, Lat: {Latitude}, Lon: {Longitude}, RelAlt: {RelativeAltitude}";
    }
}