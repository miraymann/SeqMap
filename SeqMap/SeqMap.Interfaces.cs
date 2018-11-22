using StructureMap.Pipeline;

namespace SeqMap
{
	public static partial class SeqMap
	{
		public interface IAddOrUseWizardStep<in TContract>
		{
			ISetNameOrNextItemWizardStep<TContract> AddSequence();
			ISetNameOrNextItemWizardStep<TContract> UseSequence();
		}

		public interface ISetNextItemWizardStep<in TContract>
		{
			IChooseCtorParamOrSetNextItemWizardStep<TContract> AddNext<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract;

			ISetNextItemWizardStep<TContract> NextIs<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract;
			
			ISetNextItemWizardStep<TContract> NextIsNamed(
				string name,
				params string[] profiles);

			ISetNextItemWizardStep<TContract> NextIsNamed<TImplementation>(
				string name,
				params string[] profiles)
				where TImplementation : TContract;

			void End();
		}

		public interface ISetNameOrNextItemWizardStep<in TContract>
			: ISetNextItemWizardStep<TContract>
		{
			ISetNextItemWizardStep<TContract> Named(string name);
		}

		public interface IChooseCtorParamOrSetNextItemWizardStep<in TContract>
			: ISetNextItemWizardStep<TContract>
		{
			ISetCtorArgWizardStep<TContract, TParam> Ctor<TParam>();
			ISetCtorArgWizardStep<TContract, TParam> Ctor<TParam>(string paramName);
		}

		public interface ISetCtorArgWizardStep<in TContract, in TParam>
		{
			IChooseCtorParamOrSetNextItemWizardStep<TContract> IsNamedInstance(string name);
			IChooseCtorParamOrSetNextItemWizardStep<TContract> Is(Instance instance);
			IChooseCtorParamOrSetNextItemWizardStep<TContract> Is(TParam value);
			IChooseCtorParamOrSetNextItemWizardStep<TContract> Is<TParamImplementation>() 
				where TParamImplementation : TParam;
		}
	}
}
