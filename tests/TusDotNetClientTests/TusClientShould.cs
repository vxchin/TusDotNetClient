using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Shouldly;
using TusDotNetClient;
using Xunit;

namespace TusDotNetClientTests
{
    public class TusClientShould : IDisposable
    {
        private readonly Process _tusProcess;

        public TusClientShould()
        {
            Directory.CreateDirectory("data").Delete(true);
            _tusProcess = Process.Start(new DirectoryInfo(Directory.GetCurrentDirectory())
                                           .Parent?
                                           .Parent?
                                           .Parent?
                                           .EnumerateFiles("tusd*")
                                           .First()
                                           .FullName ??
                                       throw new ArgumentException(
                                           "tusd executable must be present in test project directory"));
        }

        [Fact]
        public void CreateFileEntryOnDiskWhenCallingCreate()
        {
            var data = Guid.NewGuid().ToString();
            File.WriteAllText("data.txt", data);
            var sut = new TusClient();
            
            
            var url = sut.Create(
                "http://localhost:1080/files/",
                new FileInfo("data.txt"));
            
            
            File.Exists(Path.Combine("data", $"{url.Split('/').Last()}.bin")).ShouldBe(true);
        }

        public void Dispose()
        {
            _tusProcess.Kill();
        }
    }
}