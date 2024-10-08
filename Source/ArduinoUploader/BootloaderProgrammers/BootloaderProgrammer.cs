﻿using System;
using System.Linq;
using ArduinoUploader.Hardware;
using ArduinoUploader.Hardware.Memory;
using IntelHexFormatReader.Model;
using Microsoft.Extensions.Logging;

namespace ArduinoUploader.BootloaderProgrammers
{
    internal abstract class BootloaderProgrammer : IBootloaderProgrammer
    {
        protected ILogger Logger => ArduinoSketchUploader.Logger;

        protected IMcu Mcu { get; }

        protected BootloaderProgrammer(IMcu mcu)
        {
            Mcu = mcu;
        }

        public abstract void Open();
        public abstract void Close();
        public abstract void EstablishSync();
        public abstract void CheckDeviceSignature();
        public abstract void InitializeDevice();
        public abstract void EnableProgrammingMode();
        public abstract void LeaveProgrammingMode();
        public abstract void LoadAddress(IMemory memory, int offset);
        public abstract void ExecuteWritePage(IMemory memory, int offset, byte[] bytes);
        public abstract byte[] ExecuteReadPage(IMemory memory);

        public virtual void ProgramDevice(MemoryBlock memoryBlock, IProgress<double> progress = null)
        {
            var sizeToWrite = memoryBlock.HighestModifiedOffset + 1;
            var flashMem = Mcu.Flash;
            var pageSize = flashMem.PageSize;
            Logger?.LogInformation($"Preparing to write {sizeToWrite} bytes...");
            Logger?.LogInformation($"Flash page size: {pageSize}.");

            int offset;
            for (offset = 0; offset < sizeToWrite; offset += pageSize)
            {
                progress?.Report((double) offset / (sizeToWrite * 2));

                var needsWrite = false;
                for (var i = offset; i < offset + pageSize; i++)
                {
                    if (!memoryBlock.Cells[i].Modified) continue;
                    needsWrite = true;
                    break;
                }
                if (needsWrite)
                {
                    var bytesToCopy = memoryBlock.Cells.Skip(offset).Take(pageSize).Select(x => x.Value).ToArray();
                    Logger?.LogTrace($"Writing page at offset {offset}.");
                    LoadAddress(flashMem, offset);
                    ExecuteWritePage(flashMem, offset, bytesToCopy);
                }
                else
                {
                    Logger?.LogTrace("Skip writing page...");
                }
            }
            Logger?.LogInformation($"{sizeToWrite} bytes written to flash memory!");
        }

        public virtual void VerifyProgram(MemoryBlock memoryBlock, IProgress<double> progress = null)
        {
            var sizeToVerify = memoryBlock.HighestModifiedOffset + 1;
            var flashMem = Mcu.Flash;
            var pageSize = flashMem.PageSize;
            Logger?.LogInformation($"Preparing to verify {sizeToVerify} bytes...");
            Logger?.LogInformation($"Flash page size: {pageSize}.");

            int offset;
            for (offset = 0; offset < sizeToVerify; offset += pageSize)
            {
                progress?.Report((double) (sizeToVerify + offset) / (sizeToVerify * 2));
                Logger?.LogDebug($"Executing verification of bytes @ address {offset} (page size {pageSize})...");
                var bytesToVerify = memoryBlock.Cells.Skip(offset).Take(pageSize).Select(x => x.Value).ToArray();
                LoadAddress(flashMem, offset);
                var bytesPresent = ExecuteReadPage(flashMem);
                var succeeded = bytesToVerify.SequenceEqual(bytesPresent);
                if (succeeded) continue;

                Logger?.LogInformation(
                    $"Expected: {BitConverter.ToString(bytesToVerify)}."
                    + $"{Environment.NewLine}Read after write: {BitConverter.ToString(bytesPresent)}");
                throw new ArduinoUploaderException("Difference encountered during verification!");
            }
            
            progress?.Report(1); //Really should  fix the math to it reports 100%
            Logger?.LogInformation($"{sizeToVerify} bytes verified!");
        }
    }
}