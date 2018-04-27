using System;
using System.Collections.Generic;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hagar.TestKit
{
    [Trait("Category", "BVT")]
    public abstract class FieldCodecTester<TField, TCodec> where TCodec : class, IFieldCodec<TField>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SessionPool sessionPool;
        
        protected FieldCodecTester()
        {
            var services = new ServiceCollection();
            services.AddHagar();
            services.AddSingleton<TCodec>();
            services.AddSingleton<IFieldCodec<TField>>(sp => sp.GetRequiredService<TCodec>());

            // ReSharper disable once VirtualMemberCallInConstructor
            this.ConfigureServices(services);

            this.serviceProvider = services.BuildServiceProvider();
            this.sessionPool = this.serviceProvider.GetService<SessionPool>();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        protected virtual SerializerSession CreateSession() => this.sessionPool.GetSession();
        protected virtual TCodec CreateCodec() => this.serviceProvider.GetRequiredService<TCodec>();
        protected abstract TField CreateValue();
        protected virtual bool Equals(TField left, TField right) => EqualityComparer<TField>.Default.Equals(left, right);

        [Fact]
        public void CreateValueShouldReturnDistinctValues()
        {
            var left = this.CreateValue();
            var right = this.CreateValue();
            Assert.False(this.Equals(left, right), $"{nameof(FieldCodecTester<TField, TCodec>)}.{nameof(this.CreateValue)}() must return distinct values. Got: {left} and {right}.");
        }

        [Fact]
        public void CorrectlyAdvancesReferenceCounter()
        {
            var writer = new Writer();
            var writerSession = CreateSession();
            var writerCodec = this.CreateCodec();
            var beforeReference = writerSession.ReferencedObjects.CurrentReferenceId;

            // Write the field. This should involve marking at least one reference in the session.
            writerCodec.WriteField(writer, writerSession, 0, typeof(TField), this.CreateValue());
            var afterReference = writerSession.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Writing a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");

            var reader = new Reader(writer.ToBytes());
            var readerSession = CreateSession();
            var readerCodec = this.CreateCodec();
            var readField = reader.ReadFieldHeader(readerSession);
            beforeReference = readerSession.ReferencedObjects.CurrentReferenceId;
            readerCodec.ReadValue(reader, readerSession, readField);
            afterReference = readerSession.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Reading a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
        }

        [Fact]
        public void RoundTrippedValuesEqual()
        {
            var original = this.CreateValue();

            var writer = new Writer();
            var writerSession = CreateSession();
            var writerCodec = this.CreateCodec();
            writerCodec.WriteField(writer, writerSession, 0, typeof(TField), original);

            var reader = new Reader(writer.ToBytes());
            var readerSession = CreateSession();
            var readerCodec = this.CreateCodec();
            var readField = reader.ReadFieldHeader(readerSession);
            var deserialized = readerCodec.ReadValue(reader, readerSession, readField);
            Assert.True(this.Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
        }
    }
}
