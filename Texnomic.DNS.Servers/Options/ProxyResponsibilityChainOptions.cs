﻿using PipelineNet.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Texnomic.DNS.Abstractions;

namespace Texnomic.DNS.Servers.Options
{
    public class ProxyResponsibilityChainOptions
    {
        public List<string> Middlewares { get; set; }

        public List<Type> GetMiddlewares()
        {
            return Middlewares.Select(Middleware => Assembly.GetExecutingAssembly()
                                                            .GetTypes()
                                                            .Where(Type => Type.GetInterfaces()
                                                                               .Contains(typeof(IAsyncMiddleware<IMessage, IMessage>)))
                                                            .Single(Type => Type.Name == Middleware))
                                                            .ToList();
        }
    }
}
