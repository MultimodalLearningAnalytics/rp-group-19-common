using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using System;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.Distraction
{
    public class DeblurTimeEmitter : ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;

        public Emitter<int> OutDeblurTimes { get; private set; }
        public Emitter<string> OutInattentive { get; private set; }

        public DeblurTimeEmitter(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.OutDeblurTimes = pipeline.CreateEmitter<int>(this, nameof(this.OutDeblurTimes));
            this.OutInattentive = pipeline.CreateEmitter<string>(this, nameof(this.OutInattentive));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void WriteDeblurTimes(string name, PsiExporter store)
        {
            this.OutDeblurTimes.Write(name, store);
        }

        public void WriteInattentive(string name, PsiExporter store)
        {
            this.OutInattentive.Write(name, store);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
            this.Dispose();
        }

        public void Dispose()
        {
        }
    }
}
