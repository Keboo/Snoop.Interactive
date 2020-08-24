using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Server;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Snoop.Interactive
{
    public static class Interactive
    {
        private const string NamedPipeName = "Snoop.Interactive";


        private static MethodInfo CallbackMethod { get; }
            = typeof(Interactive).GetMethod(nameof(OnStart));

        public static void Start(int pid)
        {
            InjectorLauncherManager.Launch(pid, CallbackMethod);
        }

        private static CompositeKernel _Kernel;

        public static int OnStart(string settingsFile)
        {
            using var fs = File.OpenWrite("SUCCESS.txt");
            using var sw = new StreamWriter(fs);

            try
            {
                AttachAssemblyResolveHandler(AppDomain.CurrentDomain, x => sw.WriteLine(x));
                DoDotnetInteractiveStuff(x => sw.WriteLine(x)).Wait();
            }
            catch (Exception ex)
            {
                sw.WriteLine(ex.ToString());
            }

            return 42;
        }

        private static void AttachAssemblyResolveHandler(AppDomain domain, Action<string> log)
        {
            domain.AssemblyResolve += (sender, args) =>
            {
                var snoopInteractive = Assembly.GetExecutingAssembly();
                log($"Loading {args.Name}");
                if (args.Name.StartsWith("Snoop.Interactive,"))
                {
                    log($"Using local assembly");
                    return snoopInteractive;
                }

                var name = new AssemblyName(args.Name);
                var dllPath = Path.Combine(Path.GetDirectoryName(snoopInteractive.Location), $"{name.Name}.dll");
                if (File.Exists(dllPath))
                {
                    log($"Found dll {dllPath}");
                    return Assembly.LoadFrom(dllPath);
                }
                else
                {
                    log($"Failed to find assembly {dllPath}");
                }

                return null;
            };
        }

        private static async Task DoDotnetInteractiveStuff(Action<string> log)
        {
            _Kernel = new CompositeKernel();
            _Kernel.UseLog();

            AddDispatcherCommand(_Kernel);

            CSharpKernel csharpKernel = RegisterCSharpKernel();

            //_ = Task.Run(async () =>
            //{
            try
            {
                //Load WPF app assembly 
                await csharpKernel.SendAsync(new SubmitCode(@$"#r ""{typeof(Interactive).Assembly.Location}""
using {nameof(Snoop)}.{nameof(Snoop.Interactive)};"));
                //Add the WPF app as a variable that can be accessed
                //await csharpKernel.SetVariableAsync("App", Application.Current);

                //Start named pipe
                _Kernel.UseNamedPipeKernelServer(NamedPipeName, new DirectoryInfo("."));
                log($"Started named pipe {NamedPipeName}");
            }
            catch (Exception e)
            {
                //log(e.ToString());
                throw;
            }
            //});
        }

        private static void AddDispatcherCommand(Kernel kernel)
        {
            var dispatcherCommand = new Command("#!dispatcher", "Enable or disable running code on the Dispatcher")
            {
                new Option<bool>("--enabled", getDefaultValue:() => true)
            };
            dispatcherCommand.Handler = CommandHandler.Create<bool>(enabled =>
            {
                //RunOnDispatcher = enabled;
            });
            kernel.AddDirective(dispatcherCommand);
        }

        private static CSharpKernel RegisterCSharpKernel()
        {
            var csharpKernel = new CSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseDotNetVariableSharing();
            //This is added locally
            //.UseWpf();

            _Kernel.Add(csharpKernel);

            csharpKernel.AddMiddleware(async (KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next) =>
            {
                //if (RunOnDispatcher)
                //{
                //    await Dispatcher.InvokeAsync(async () => await next(command, context));
                //}
                //else
                //{
                await next(command, context);
                //}
            });

            return csharpKernel;
        }
    }
}
