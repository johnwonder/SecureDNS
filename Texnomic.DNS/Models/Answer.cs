﻿using BinarySerialization;
using System;
using System.Net;
using Texnomic.DNS.Converters;
using Texnomic.DNS.Enums;
using Texnomic.DNS.Records;

namespace Texnomic.DNS.Models
{
    public class Answer : Question
    {
        [FieldOrder(3)]
        [FieldBitLength(32)]
        [FieldEndianness(Endianness.Big)]
        public uint TTL { get; set; }

        [Ignore]
        public TimeSpan TimeToLive => new TimeSpan(0, 0, (int)TTL);

        [FieldOrder(4)]
        [FieldBitLength(16)]
        [FieldEndianness(Endianness.Big)]
        public ushort Length { get; set; }

        [FieldOrder(5)]
        [FieldLength(nameof(Length))]
        [SubtypeFactory(nameof(Type), typeof(RecordFactory))]
        public IRecord Record { get; set; }
    }

}