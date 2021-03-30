using Hagar;
using System;
using System.Threading.Tasks;
using TestRpc.Runtime;

namespace TestRpc.App.Two
{
    public interface IPingGrain : IGrain
    {
        ValueTask Ping();
        void VoidMethod();
    }
}

namespace TestRpc.App
{
    public interface IPingGrain : IGrain
    {
        ValueTask Ping();
    }

    public interface IPingPongGrain : IPingGrain
    {
        ValueTask<string> Echo(string input);
    }

    namespace Generic.EdgeCases
    {
        public interface IBasicGrain : IGrain
        {
            Task<string> Hello();
            Task<string[]> ConcreteGenArgTypeNames();
        }

        public interface IGrainWithTwoGenArgs<T1, T2> : IBasicGrain
        {
            Task Ping(T1 input);
            Task Ping(T2 input, T1 input2);
            
            // TODO: Disambiguate these cases
            //Task Ping(T2 input);
            //Task Ping(T1 input, T2 input2);
            // Generate generic local functions to disambiguate calls, like so:
            /*
            private static Task Test()
            {
                ISpecializedInterface2<string> z = null;
                return Disambiguate1(z, 5);
                static Task Disambiguate1<T>(IGrainWithTwoGenArgs<int, T> x, int a) => x.Ping(a);
                static Task Disambiguate2<T>(IGrainWithTwoGenArgs<T, int> x, int a) => x.Ping(a);
                static Task Disambiguate3<T>(IGrainWithTwoGenArgs<T, int> x, int a, T b) => x.Ping(a, b);
                static Task Disambiguate4<T>(IGrainWithTwoGenArgs<int, T> x, int a, T b) => x.Ping(a, b);
            }
            */
        }

        public interface IGrainWithThreeGenArgs<T1, T2, T3> : IBasicGrain
        { }

        public interface IGrainReceivingRepeatedGenArgs<T1, T2> : IBasicGrain
        { }


        public interface IPartiallySpecifyingInterface<T> : IGrainWithTwoGenArgs<T, int>
        {
            Task<T> ReturnT();
        }

        public interface ISpecializedInterface<T> : IGrainWithTwoGenArgs<string, int>
        {
            Task<T> ReturnT();
        }

        public interface ISpecializedInterface2<T> : IGrainWithTwoGenArgs<int, int>
        {
            Task<T> ReturnT();
        }

        public interface IReceivingRepeatedGenArgsAmongstOthers<T1, T2, T3> : IBasicGrain
        {
            Task<T1> GetT1();
            Task<T3> Get(T1 a, T2 b);
            Task<T1> Get(T2 a, T2 b);
            Task<T1> Get(T3 a, T1 b);
        }


        public interface IReceivingRepeatedGenArgsFromOtherInterface<T1, T2, T3> : IBasicGrain
        { }

        public interface ISpecifyingGenArgsRepeatedlyToParentInterface<T> : IReceivingRepeatedGenArgsFromOtherInterface<T, T, T>
        { }


        public interface IReceivingRearrangedGenArgs<T1, T2> : IBasicGrain
        { }

        public interface IReceivingRearrangedGenArgsViaCast<T1, T2> : IBasicGrain
        { }

        public interface ISpecifyingRearrangedGenArgsToParentInterface<T1, T2> : IReceivingRearrangedGenArgsViaCast<T2, T1>
        { }

        public interface IArbitraryInterface<T1, T2> : IBasicGrain
        { }

        public interface IInterfaceUnrelatedToConcreteGenArgs<T> : IBasicGrain
        { }


        public interface IInterfaceTakingFurtherSpecializedGenArg<T> : IBasicGrain
        { }


        public interface IAnotherReceivingFurtherSpecializedGenArg<T> : IBasicGrain
        { }

        public interface IYetOneMoreReceivingFurtherSpecializedGenArg<T> : IBasicGrain
        { }

        public interface IHungryGrain<T> : IGrain
        {
            Task Eat(T food);

            Task EatWith<U>(T food, U condiment);
        }

        public interface IOmnivoreGrain : IGrain
        {
            Task Eat<T>(T food);
        }

        [Serializable]
        [GenerateSerializer]
        public class Apple { }

        public interface ICaterpillarGrain : IHungryGrain<Apple>, IOmnivoreGrain
        {
            new Task Eat<T>(T food);
        }
    }
}