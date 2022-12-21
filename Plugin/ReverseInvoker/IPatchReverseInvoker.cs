using System;

namespace UniTASPlugin.ReverseInvoker;

public interface IPatchReverseInvoker
{
    bool Invoking { get; }

    TRet Invoke<TRet>(Func<TRet> method);
    TRet Invoke<TRet, T>(Func<T, TRet> method, T arg1);
    TRet Invoke<TRet, T1, T2>(Func<T1, T2, TRet> method, T1 arg1, T2 arg2);

    void SetProperty<T>(Action<T> property, T value);
    T GetProperty<T>(Func<T> property);
}