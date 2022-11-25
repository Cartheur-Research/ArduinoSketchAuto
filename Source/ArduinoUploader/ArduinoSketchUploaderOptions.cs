using ArduinoUploader.Hardware;

namespace ArduinoUploader
{
    public class ArduinoSketchUploaderOptions
    {
        public bool LoadFromEmbeddedResource { get; set; } = false;  

        public string FileName { get; set; }

        public string PortName { get; set; }

        public ArduinoModel ArduinoModel { get; set; }
    }
}