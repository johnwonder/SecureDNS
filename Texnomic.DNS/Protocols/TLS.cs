﻿using System;
using Nerdbank.Streams;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BinarySerialization;
using Texnomic.DNS.Abstractions;
using Texnomic.DNS.Models;
using Texnomic.DNS.Extensions;
using Texnomic.DNS.Options;
using Microsoft.Extensions.Options;

namespace Texnomic.DNS.Protocols
{
    public class TLS : Protocol
    {
        private readonly IOptionsMonitor<TLSOptions> Options;
        private readonly TcpClient TcpClient;

        private SslStream SslStream;
        private PipeReader PipeReader;
        private PipeWriter PipeWriter;

        public TLS(IOptionsMonitor<TLSOptions> TLSOptions)
        {
            Options = TLSOptions;

            TcpClient = new TcpClient();
        }

        private async Task InitializeAsync()
        {
            await TcpClient.ConnectAsync(Options.CurrentValue.Host, Options.CurrentValue.Port);

            SslStream = new SslStream(TcpClient.GetStream(), true, ValidateServerCertificate)
            {
                ReadTimeout = Options.CurrentValue.Timeout,
                WriteTimeout = Options.CurrentValue.Timeout
            };

            await SslStream.AuthenticateAsClientAsync(Options.CurrentValue.Host);

            PipeReader = SslStream.UsePipeReader();

            PipeWriter = SslStream.UsePipeWriter();
        }

        public override async Task<byte[]> ResolveAsync(byte[] Query)
        {
            if (!TcpClient.Connected || !SslStream.CanWrite) await InitializeAsync();

            Query = PrefixLength(ref Query);

            await PipeWriter.WriteAsync(Query);

            PipeWriter.Complete();

            var Task = PipeReader.ReadAsync().AsTask();

            Task.Wait(Options.CurrentValue.Timeout);

            if (Task.IsCompleted)
            {
                var Result = Task.Result;

                PipeReader.Complete();

                var Buffer = Result.Buffer.Length > 14
                    ? Result.Buffer.Slice(2)
                    : throw new OperationCanceledException();

                return Buffer.ToArray();
            }

            PipeReader.CancelPendingRead();

            PipeReader.Complete();

            throw new TimeoutException();
        }


        private bool ValidateServerCertificate(object Sender, X509Certificate Certificate, X509Chain Chain, SslPolicyErrors SslPolicyErrors)
        {
            return string.IsNullOrEmpty(Options.CurrentValue.PublicKey) ? SslPolicyErrors == SslPolicyErrors.None : SslPolicyErrors == SslPolicyErrors.None && Certificate.GetPublicKeyString() == Options.CurrentValue.PublicKey;
        }

        private static byte[] PrefixLength(ref byte[] Query)
        {
            var Length = BitConverter.GetBytes((ushort)Query.Length);

            Array.Reverse(Length);

            var Buffer = new byte[Query.Length + 2];

            Array.Copy(Length, Buffer, 2);

            Array.Copy(Query, 0, Buffer, 2, Query.Length);

            return Buffer;
        }

        protected override void Dispose(bool Disposing)
        {
            if (IsDisposed) return;

            if (Disposing)
            {
                SslStream.Dispose();
                TcpClient.Dispose();
            }

            IsDisposed = true;
        }
    }
}
