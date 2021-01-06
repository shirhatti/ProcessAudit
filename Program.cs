using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Rather than tracking start and end,
// we are assuming only one process at a time
var startTime = DateTime.Now;
var requestStartTimes = new List<double>();

using var session = new TraceEventSession("ProcessAudit");

var options = new TraceEventProviderOptions();
var processNames = new List<string> { "iisexpress", "Timings" };
options.ProcessNameFilter = processNames;

session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);
session.Source.Kernel.ProcessStart += (data) =>
    {
        if (processNames.Contains(data.ProcessName))
        {
            Console.WriteLine($"{data.EventName,-30}{data.TimeStamp:O}{data.ProcessName,30}");
            startTime = data.TimeStamp;
        }

    };

session.EnableProvider("Microsoft.AspNetCore.Hosting", TraceEventLevel.Always, ulong.MaxValue);
session.Source.Dynamic.AddCallbackForProviderEvents((providerName, eventName) =>
    {
        if (providerName != "Microsoft.AspNetCore.Hosting")
        {
            return EventFilterResponse.RejectProvider;
        }
        if (eventName == "HostStart/Start" || eventName == "RequestStart/Start")
        {
            return EventFilterResponse.AcceptEvent;
        }
        return EventFilterResponse.RejectEvent;
    },
    (data) =>
    {
        Console.WriteLine($"{data.EventName,-30}{data.TimeStamp:O}{data.ProcessName,30}");
        if (data.EventName == "RequestStart/Start")
        {
            requestStartTimes.Add(data.TimeStamp.Subtract(startTime).TotalMilliseconds);
        }
    });

Task.Run(() => session.Source.Process());

while (true)
{
    var key = Console.ReadKey(true);
    Console.WriteLine();
    switch (key.KeyChar)
    {
        case 'p':
            Console.WriteLine(string.Join(",", requestStartTimes));
            break;
        case 'q':
            return;
        default:
            Console.WriteLine("Unknown option, exiting");
            return;
    }
}