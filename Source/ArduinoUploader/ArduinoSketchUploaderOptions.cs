using ArduinoUploader.Hardware;

namespace ArduinoUploader
{
    public class ArduinoSketchUploaderOptions
    {
        public bool LoadFromEmbeddedResource { get; set; } = true;  

        public string FileName { get; set; }

        public string PortName { get; set; }

        public ArduinoModel ArduinoModel { get; set; }
    }
}