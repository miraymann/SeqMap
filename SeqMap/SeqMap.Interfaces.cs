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
			ISetProfileOrChooseCtorParamOrSetNextItemWizardStep<TContract> AddNext<TImplementation>()
				where TImplementation : TContract;

			ISetProfilesOrNextItemWitzardStep<TContract> NextIs<TImplementation>()
				where TImplementation : TContract;

			ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed(string name);

			ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed<TImplementation>(string name)
				where TImplementation : TContract;

			void End();
		}

		public interface ISetNameOrNextItemWizardStep<in TContract>
			: ISetNextItemWizardStep<TContract>
		{
			ISetNextItemWizardStep<TContract> Named(string name);
		}

		public interface ISetProfilesOrNextItemWitzardStep<in TContract>
			: ISetNextItemWizardStep<TContract>
		{
			ISetNextItemWizardStep<TContract> WhenProfileIs(string profile);

			ISetNextItemWizardStep<TContract> WhenProfileIsIn(
				string firtsProfile,
				string secondProfile,
				params string[] otherProfiles);
		}

		public interface ISetProfileOrChooseCtorParamOrSetNextItemWizardStep<in TContract>
			: IChooseCtorParamOrSetNextItemWizardStep<TContract>
		{
			IChooseCtorParamOrSetNextItemWizardStep<TContract> IntoProfile(string profile);

			IChooseCtorParamOrSetNextItemWizardStep<TContract> IntoProfiles(
				string firtsProfile,
				string secondProfile,
				params string[] otherProfiles);
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
