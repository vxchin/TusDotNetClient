using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Shouldly;
using TusDotNetClient;
using Xunit;

namespace TusDotNetClientTests
{
    public class TusClientTests : IDisposable
    {
        private readonly Process _tusProcess;

        public TusClientTests()
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
        public void AfterCallingCreate_DataShouldContainAFile()
        {
            var data = Guid.NewGuid().ToString();
            File.WriteAllText("data.txt", data);
            var sut = new TusClient();
            
            
            var url = sut.Create(
                "http://localhost:1080/files/",
                new FileInfo("data.txt"));
            
            
            var upload = new FileInfo(Path.Combine("data", $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(0);
        }

        public void Dispose()
        {
            _tusProcess.Kill();
        }
    }
}