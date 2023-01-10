using Microsoft.Extensions.Logging;
using ArduinoUploader;
using ArduinoUploader.Hardware;
using Fclp;
using Serilog;
using System;

namespace ArduinoUploadTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            var p = new FluentCommandLineParser<ApplicationArguments>();

            p.Setup(arg => arg.PortName)
             .As('p', "port")
             .Required();

            p.Setup<ArduinoModel>(arg => arg.ArduinoModel)
             .As('m', "model")
             .Required();

            p.Setup<string>(arg => arg.FileName)
             .As('f', "file")
             .Required();

            p.Setup(arg => arg.Silent)
             .As('s', "silent")
             .SetDefault(false);

            p.SetupHelp("?", "help")
             .Callback(text =>
             {
                 Console.WriteLine(text);
                 Environment.Exit(0);
             });

            var result = p.Parse(args);

            if (result.HasErrors == false)
            {
                var appArgs = p.Object;
                var uploaderOptions = new ArduinoSketchUploaderOptions()
                {
                    PortName = appArgs.PortName,
                    ArduinoModel = appArgs.ArduinoModel,
                    FileName = appArgs.FileName,
                };

                Log.Information("Uploading file {file} to Arduino {model} using {port}", appArgs.FileName, appArgs.ArduinoModel, appArgs.PortName);

                Progress<double> progress = new Progress<double>(
                    p =>
                    {
                        Log.Information("Upload progress: {progress:P}%", p);
                        Console.CursorTop--;
                    });
                
                var uploader = new ArduinoSketchUploader(uploaderOptions, null, appArgs.Silent ? null : progress);
                uploader.UploadFile(uploaderOptions.FileName);

                Log.Information("Upload complete");
            }
            else
            {
                Log.Error(result.ErrorText);
            }
        }
    }

    public class ApplicationArguments
    {
        public bool Silent { get; set; }
        public string PortName { get; set; } = "";
        public string FileName { get; set; } = "";
        public ArduinoModel ArduinoModel { get; set; }
    }
}