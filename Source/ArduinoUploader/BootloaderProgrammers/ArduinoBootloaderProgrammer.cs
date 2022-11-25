﻿using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using ArduinoUploader.BootloaderProgrammers.Protocols;
using ArduinoUploader.Hardware;
using RJCP.IO.Ports;

namespace ArduinoUploader.BootloaderProgrammers
{
    internal abstract class ArduinoBootloaderProgrammer : BootloaderProgrammer
    {
        protected SerialPortConfig SerialPortConfig;

        protected ArduinoBootloaderProgrammer(SerialPortConfig serialPortConfig, IMcu mcu)
            : base(mcu)
        {
            SerialPortConfig = serialPortConfig;
        }

        protected SerialPortStream SerialPort { get; set; }

        public override void Open()
        {
            var portName = SerialPortConfig.PortName;
            var baudRate = SerialPortConfig.BaudRate;
            Logger?.LogInformation($"Opening serial port {portName} - baudrate {baudRate}");

            SerialPort = new SerialPortStream(portName, baudRate)
            {
                ReadTimeout = SerialPortConfig.ReadTimeOut,
                WriteTimeout = SerialPortConfig.WriteTimeOut
            };

            var preOpen = SerialPortConfig.PreOpenResetBehavior;
            if (preOpen != null)
            {
                Logger?.LogInformation($"Executing Post Open behavior ({preOpen})...");
                SerialPort = preOpen.Reset(SerialPort, SerialPortConfig);
            }

            try
            {
                SerialPort.Open();
            }
            catch (ObjectDisposedException ex)
            {
                throw new ArduinoUploaderException(
                    $"Unable to open serial port {portName} - {ex.Message}.");
            }
            catch (InvalidOperationException ex)
            {
                throw new ArduinoUploaderException(
                    $"Unable to open serial port {portName} - {ex.Message}.");
            }
            Logger?.LogTrace($"Opened serial port {portName} with baud rate {baudRate}!");

            var postOpen = SerialPortConfig.PostOpenResetBehavior;
            if (postOpen != null)
            {
                Logger?.LogInformation($"Executing Post Open behavior ({postOpen})...");
                SerialPort = postOpen.Reset(SerialPort, SerialPortConfig);
            }

            var sleepAfterOpen = SerialPortConfig.SleepAfterOpen;
            if (SerialPortConfig.SleepAfterOpen <= 0) return;

            Logger?.LogTrace($"Sleeping for {sleepAfterOpen} ms after open...");
            Thread.Sleep(sleepAfterOpen);
        }

        public override void EstablishSync()
        {
            // Do nothing.
        }

        public override void Close()
        {
            var preClose = SerialPortConfig.CloseResetAction;
            if (preClose != null)
            {
                Logger?.LogInformation("Resetting...");
                SerialPort = preClose.Reset(SerialPort, SerialPortConfig);
            }

            Logger?.LogInformation("Closing serial port...");
            SerialPort.DtrEnable = false;
            SerialPort.RtsEnable = false;
            try
            {
                SerialPort.Close();
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        protected virtual void Send(IRequest request)
        {
            var bytes = request.Bytes;
            var length = bytes.Length;
            Logger?.LogTrace($"Sending {length} bytes: {Environment.NewLine}"
                + $"{BitConverter.ToString(bytes)}");
            SerialPort.Write(bytes, 0, length);
        }

        protected TResponse Receive<TResponse>(int length = 1) 
            where TResponse : Response, new()
        {
            var bytes = ReceiveNext(length);
            if (bytes == null) return null;
            return new TResponse {Bytes = bytes};
        }

        protected int ReceiveNext()
        {
            var bytes = new byte[1];
            try
            {
                SerialPort.Read(bytes, 0, 1);
                Logger?.LogTrace($"Receiving byte: {BitConverter.ToString(bytes)}");
                return bytes[0];
            }
            catch (TimeoutException)
            {
                return -1;
            }
        }

        protected byte[] ReceiveNext(int length)
        {
            var bytes = new byte[length];
            var retrieved = 0;
            try
            {
                while (retrieved < length)
                    retrieved += SerialPort.Read(bytes, retrieved, length - retrieved);

                Logger?.LogTrace($"Receiving bytes: {BitConverter.ToString(bytes)}");
                return bytes;
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
    }
}