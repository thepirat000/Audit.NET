using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.DynamicProxy.UnitTest
{
    public interface IInterceptMe
    {
        string SomeProperty { get; set; }
        void SomeVoidMethod(int number, string text);
        string ReturnString(string text);
        event EventHandler SomeEvent;
        T GenericReturn<T>(T value);
        string GetSomePropValue();
    }

    public class InterceptMeBase : IInterceptMe
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

    }
}
