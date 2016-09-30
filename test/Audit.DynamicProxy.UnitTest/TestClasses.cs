using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.DynamicProxy.UnitTest
{
    public interface IMyRepository
    {
        int InsertUser(string userName);
        Task<int> InsertUserAsync(string userName);
    }
    public class MyRepository : IMyRepository
    {
        public int InsertUser(string userName)
        {
            Thread.Sleep(1000);
            return new Random().Next();
        }

        public async Task<int> InsertUserAsync(string userName)
        {
            await Task.Delay(1000);
            if (userName == null)
            {
                throw new ArgumentNullException(userName, "UserName cannot be null");
            }

            return new Random().Next();
        }
    }



    public interface IInterceptMe
    {
        string SomeProperty { get; set; }
        void SomeVoidMethod(int number, string text);
        string ReturnString(string text);
        event EventHandler SomeEvent;
        T GenericReturn<T>(T value);
        string GetSomePropValue();

        Task AsyncMethodAsync(string parameter);

        Task<string> AsyncMethodAsyncWithCancellation(CancellationToken cancellationToken);

        Task<string> AsyncFunctionAsync(string parameter);
        Task<string> MethodThatReturnsATask(string parameter);
        void AsyncReturningVoidAsync(string parameter);

        void OutParam(string s, out int i, Func<int> ignoreMe);
        string RefParam(string s, ref int i, Func<int> ignoreMe);
    }

    public abstract class InterceptMeBase : IInterceptMe
    {
        public virtual string SomeProperty { get; set; }
        public virtual event EventHandler SomeEvent;
        public virtual T GenericReturn<T>(T value)
        {
            return default(T);
        }

        public virtual string ReturnString(string text)
        {
            return null;
        }

        public virtual void SomeVoidMethod(int number, string text)
        {
        }

        public virtual string GetSomePropValue()
        {
            return "base";
        }

        public abstract Task AsyncMethodAsync(string parameter);
        public abstract Task<string> AsyncFunctionAsync(string parameter);
        public abstract Task<string> MethodThatReturnsATask(string parameter);
        public abstract void AsyncReturningVoidAsync(string parameter);
        public abstract Task<string> AsyncMethodAsyncWithCancellation(CancellationToken cancellationToken);

        public abstract void OutParam([AuditIgnore]string s, out int i, [AuditIgnore]Func<int> ignoreMe);
        public abstract string RefParam(string s, ref int i, Func<int> ignoreMe);

    }
    public class InterceptMe : InterceptMeBase
    {
        public override event EventHandler SomeEvent;

        public InterceptMe()
        {

        }
        public InterceptMe(string value)
        {
            SomeProperty = value;
        }

        public override string SomeProperty { get; set; }

        public override string ReturnString(string text)
        {
            return text.ToUpper();
        }

        public override void SomeVoidMethod(int number, string text)
        {

        }

        public override T GenericReturn<T>(T value)
        {
            return value;
        }

        public virtual string IamVirtual()
        {
            return SomeProperty;
        }

        [AuditIgnore]
        public override string GetSomePropValue()
        {
            return SomeProperty;
        }

        public override Task<string> MethodThatReturnsATask(string parameter)
        {
            return new Task<string>(() => "OK");
        }

        public override async Task<string> AsyncFunctionAsync(string parameter)
        {
            var scopeBefore = AuditProxy.CurrentScope;
            System.Diagnostics.Trace.WriteLine("AsyncFunctionAsync - Before await.");
            await Task.Delay(int.Parse(parameter));
            System.Diagnostics.Trace.WriteLine("AsyncFunctionAsync - After await.");
            var scopeAfter = AuditProxy.CurrentScope;
            return "ok";
        }

        public override async Task AsyncMethodAsync(string parameter)
        {
            var scopeBefore = AuditProxy.CurrentScope;
            System.Diagnostics.Trace.WriteLine("AsyncMethodAsync - Before await.");
            await Task.Delay(int.Parse(parameter));
            System.Diagnostics.Trace.WriteLine("AsyncMethodAsync - After await.");
            var scopeAfter = AuditProxy.CurrentScope;
        }

        public override async void AsyncReturningVoidAsync(string parameter)
        {
            var scopeBefore = AuditProxy.CurrentScope;
            await Task.Delay(int.Parse(parameter));
            var scopeAfter = AuditProxy.CurrentScope;
        }

        public override async Task<string> AsyncMethodAsyncWithCancellation(CancellationToken ct)
        {
            while (true)
            {
                Thread.Sleep(100);
                ct.ThrowIfCancellationRequested();
            }
            return "impossible";
        }

        public override void OutParam(string s, out int i, Func<int> ignoreMe)
        {
            i = 22;

        }

        [return:AuditIgnore]
        public override string RefParam(string s, ref int i, [AuditIgnore] Func<int> ignoreMe)
        {
            i = ++i;
            return "should be ignored";
        }


    }
}
