using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Npgsql.Logging;

namespace Npgsql
{
    static class Counters
    {
        /// <summary>
        /// The number of connections per second that are being made to a database server.
        /// </summary>
        internal static Counter HardConnectsPerSecond;
        /// <summary>
        /// The number of disconnects per second that are being made to a database server.
        /// </summary>
        internal static Counter HardDisconnectsPerSecond;
        /// <summary>
        /// The total number of connection pools.
        /// </summary>
        internal static Counter NumberOfActiveConnectionPools;
        /// <summary>
        /// The number of (pooled) active connections that are currently in use.
        /// </summary>
        internal static Counter NumberOfActiveConnections;
        /// <summary>
        /// The number of connections available for use in the connection pools.
        /// </summary>
        internal static Counter NumberOfFreeConnections;
        /// <summary>
        /// The number of active connections that are not pooled.
        /// </summary>
        internal static Counter NumberOfNonPooledConnections;
        /// <summary>
        /// The number of active connections that are being managed by the connection pooling infrastructure.
        /// </summary>
        internal static Counter NumberOfPooledConnections;
        /// <summary>
        /// The number of active connections being pulled from the connection pool.
        /// </summary>
        internal static Counter SoftConnectsPerSecond;
        /// <summary>
        /// The number of active connections that are being returned to the connection pool.
        /// </summary>
        internal static Counter SoftDisconnectsPerSecond;

        static bool _initialized;
        static readonly object InitLock = new object();

        static readonly NpgsqlLogger Log = NpgsqlLogManager.GetCurrentClassLogger();

#pragma warning disable CA1801 // Review unused parameters
        internal static void Initialize(bool usePerfCounters)
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;
                _initialized = true;
                var enabled = false;
                var expensiveEnabled = false;

                try
                {
                    if (usePerfCounters)
                    {
                        enabled = true;
                        expensiveEnabled = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Debug("Exception while checking for performance counter category (counters will be disabled)", e);
                }
                try
                {
                    HardConnectsPerSecond = new Counter(enabled, nameof(HardConnectsPerSecond), CustomMetricsEventSource.Log);
                    HardDisconnectsPerSecond = new Counter(enabled, nameof(HardDisconnectsPerSecond), CustomMetricsEventSource.Log);
                    NumberOfActiveConnectionPools = new Counter(enabled, nameof(NumberOfActiveConnectionPools), CustomMetricsEventSource.Log);
                    NumberOfNonPooledConnections = new Counter(enabled, nameof(NumberOfNonPooledConnections), CustomMetricsEventSource.Log);
                    NumberOfPooledConnections = new Counter(enabled, nameof(NumberOfPooledConnections), CustomMetricsEventSource.Log);
                    SoftConnectsPerSecond = new Counter(expensiveEnabled, nameof(SoftConnectsPerSecond), CustomMetricsEventSource.Log);
                    SoftDisconnectsPerSecond = new Counter(expensiveEnabled, nameof(SoftDisconnectsPerSecond), CustomMetricsEventSource.Log);
                    NumberOfActiveConnections = new Counter(expensiveEnabled, nameof(NumberOfActiveConnections), CustomMetricsEventSource.Log);
                    NumberOfFreeConnections = new Counter(expensiveEnabled, nameof(NumberOfFreeConnections), CustomMetricsEventSource.Log);
                }
                catch (Exception e)
                {
                    Log.Debug("Exception while setting up performance counter (counters will be disabled)", e);
                }
            }
        }
    }
#pragma warning restore CA1801 // Review unused parameters

    /// <summary>
    /// EventSource for create events from NpgSql 
    /// </summary>
    [EventSource(Name = "NpgSqlMetricsEventSource")]
    public sealed class CustomMetricsEventSource : EventSource
    {
        /// <summary>
        /// Log instance
        /// </summary>
        public static CustomMetricsEventSource Log = new CustomMetricsEventSource();

        /// <inheritdoc />
        CustomMetricsEventSource()
        {
        }
    }

    /// <summary>
    /// This class is currently a simple wrapper around System.Diagnostics.PerformanceCounter.
    /// Since these aren't supported in .NET Standard, all the ifdef'ing happens here.
    /// When an alternative performance counter API emerges for netstandard, it can be added here.
    /// </summary>
    sealed class Counter : EventCounter
    {
        readonly bool _isEnabled;
        private long _value;
        public long Value => _value;


        internal Counter(bool enabled, string diagnosticsCounterName, EventSource eventSource) : base(diagnosticsCounterName, eventSource)
        {
            _isEnabled = enabled;
        }

        internal void Increment()
        {
            if(_isEnabled)
                WriteMetric(Interlocked.Increment(ref _value));
        }

        internal void Decrement()
        {
            if(_isEnabled)
                WriteMetric(Interlocked.Decrement(ref _value));
        }
    }
}
