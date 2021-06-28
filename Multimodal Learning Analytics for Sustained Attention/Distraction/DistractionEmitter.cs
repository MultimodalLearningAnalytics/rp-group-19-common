using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.Distraction
{
    public class DistractionEmitter : ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        public Emitter<string> OutDistraction { get; private set; }

        public DistractionEmitter(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.OutDistraction = pipeline.CreateEmitter<string>(this, nameof(this.OutDistraction));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Write(string name, PsiExporter store)
        {
            OutDistraction.Write(name, store);
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
