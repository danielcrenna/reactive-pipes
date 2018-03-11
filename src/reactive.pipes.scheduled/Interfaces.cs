using System;

namespace reactive.pipes.scheduled
{
    public interface Method { }

    public interface Before : Method
    {
        bool Before();
    }

    public interface Handler : Method
    {
        bool Perform();
    }

    public interface After : Method
    {
        void After();
    }

    public interface Success : Method
    {
        void Success();
    }

    public interface Failure : Method
    {
        void Failure();
    }

    public interface Error : Method
    {
        void Error(Exception error);
    }

    public interface Halt : Method
    {
        void Halt(bool immediate);
    }
}
