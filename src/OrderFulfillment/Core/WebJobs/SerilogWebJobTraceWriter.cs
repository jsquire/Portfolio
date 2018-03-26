using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;
using Serilog;

namespace OrderFulfillment.Core.WebJobs
{
    /// <summary>
    ///   Serves as a trace writer for WebJob Host entries, using Serilog 
    ///   as the backing logger.
    /// </summary>
    /// 
    /// <seealso cref="Microsoft.Azure.WebJobs.Host.TraceWriter" />
    /// 
    public class SerilogWebJobTraceWriter : TraceWriter
    {
        /// <summary>
        ///   The logger to use for emitting trace events.
        /// </summary>
        /// 
        private ILogger Log { get; }
                
        /// <summary>
        ///   Initializes a new instance of the <see cref="SerilogWebJobTraceWriter" /> class.
        /// </summary>
        /// 
        /// <param name="logger">The logger to use for emitting trace events.</param>
        /// <param name="traceLevel">The trace level that will be used as a threshold for reporting telemetry.</param>        
        /// 
        public SerilogWebJobTraceWriter(ILogger    logger,
                                        TraceLevel traceLevel = TraceLevel.Warning) : base(traceLevel)
        {
            this.Log = logger;
        }

        /// <summary>
        ///   Writes a trace event.
        /// </summary>
        /// 
        /// <param name="traceEvent">The <see cref="TraceEvent" /> to trace.</param>
        /// 
        public override void Trace(TraceEvent traceEvent)
        {
        
            var exception = traceEvent.Exception;

            // Log all traces with exceptions as errors using the specific Error overload that does clean exception logging 
            
            if (exception != null)
            {
                this.Log.Error(exception, "An unhandled exception occurred of type {ExceptionTypeName}.", exception.GetType().Name);
            }

            switch (traceEvent.Level)
            {
                case TraceLevel.Verbose:
                    this.Log.Verbose(traceEvent.Message);

                    break;

                case TraceLevel.Info:
                    this.Log.Information(traceEvent.Message);

                    break;

                case TraceLevel.Warning:
                    this.Log.Warning(traceEvent.Message);

                    break;

                case TraceLevel.Error:
                    this.Log.Error(traceEvent.Message);

                    break;                    
            }
        }
    }
}
