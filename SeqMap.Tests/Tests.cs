using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureMap;

namespace SeqMap.Tests
{
    public class Tests
    {
	    [Test]
		public void EmptySequence_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .UseItems()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    CollectionAssert.IsEmpty(sequence);
		    }
		}

		[Test]
	    public void NoProfile_Test()
		{
			var registry = new Registry();
			registry.ForSequenceOf<I>()
				.AddItems()
				.AddNext<A>()
				.AddNext<D>()
				.AddNext<H>()
				.AddNext<G>()
				.AddNext<O>()
				.End();

			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var resultView = SelectResultViewFrom(sequence);
				Assert.AreEqual("ADHGO", resultView);
			}
		}

		[Test]
	    public void ItemsDublication_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .AddItems()
				.AddNext<A>()
			    .AddNext<D>()
			    .AddNext<A>()
			    .AddNext<A>()
			    .AddNext<O>()
				.AddNext<C>()
			    .AddNext<C>()
			    .AddNext<C>()
				.End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var resultView = SelectResultViewFrom(sequence);
			    Assert.AreEqual("ADAAOCCC", resultView);
		    }
	    }

		[Test]
	    public void IsIndependentOfStandardStructureMap_NoProfile_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<Z>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<L>();

			registry.ForSequenceOf<I>()
				.AddItems()
			    .AddNext<A>()
			    .AddNext<D>()
			    .AddNext<H>()
			    .AddNext<G>()
			    .AddNext<O>()
			    .End();

		    registry.For<I>().Add<V>();
		    registry.For<I>().Add<T>();
		    registry.For<I>().Add<R>();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var resultView = SelectResultViewFrom(sequence);
			    Assert.AreEqual("ADHGO", resultView);
		    }
	    }

	    [Test]
	    public void NoProfile_SingleItem_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .AddItems()
			    .AddNext<X>()
			    .End();

		    var container = new Container(registry);
		    var result = container.GetInstance<IEnumerable<I>>()
			    .ToArray();

		    Assert.AreEqual(1, result.Length);
		    Assert.AreEqual("X", result[0].Name);
		}

		[Test]
	    public void SingleProfile_Test()
	    {
		    var registry = new Registry();
			registry.ForSequenceOf<I>()
				.AddItems()
			    .AddNext<A>()
			    .AddNext<D>()
			    .AddNext<H>("Dog")
			    .AddNext<G>()
			    .AddNext<O>("Dog")
				.AddNext<X>("Dog")
				.AddNext<Z>()
				.AddNext<E>()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var resultView = SelectResultViewFrom(sequence);
			    Assert.AreEqual("ADGZE", resultView);

			    using (var dogContainer = container.GetNestedContainer("Dog"))
			    {
					var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
					var dogResultView = SelectResultViewFrom(dogSequence);
				    Assert.AreEqual("ADHGOXZE", dogResultView);
				}
		    }
	    }

		[Test]
	    public void MultiProfiles_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
				.AddItems()
			    .AddNext<A>()
			    .AddNext<B>("=", "==", "===", "====", "=====", "======", "=======")
			    .AddNext<C>("=", "==", "===", "====", "=====", "======")
			    .AddNext<G>("=", "==", "===", "====", "=====")
			    .AddNext<O>("=", "==", "===", "====")
			    .AddNext<X>("=", "==", "===")
			    .AddNext<Z>("=", "==")
			    .AddNext<E>("=")
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
				var resultView = SelectResultViewFrom(sequence);
			    Assert.AreEqual("A", resultView);

			    var expectedResult = "ABCGOXZE";
			    var profiles = Enumerable
				    .Range(1, 7)
				    .Select(i => Enumerable.Repeat("=", i))
				    .Select(o => string.Join(string.Empty, o));

				foreach (var profile in profiles)
			    {
				    using (var profileContainer = container.GetNestedContainer(profile))
				    {
					    var profileSequence = profileContainer.GetInstance<IEnumerable<I>>();
						var profileResultView = SelectResultViewFrom(profileSequence);
					    Assert.AreEqual(expectedResult, profileResultView);
				    }

				    expectedResult = expectedResult
					    .Substring(0, expectedResult.Length - 1);
			    }
		    }
		}

	    [Test]
	    public void EmptySequenceForDefaultProfile_SingleProfile_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .UseItems()
			    .AddNext<A>("Dog")
			    .AddNext<B>("Dog")
			    .AddNext<C>("Dog")
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    CollectionAssert.IsEmpty(sequence);

			    using (var dogContainer = container.GetNestedContainer("Dog"))
			    {
				    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
				    var dogResultView = SelectResultViewFrom(dogSequence);
				    Assert.AreEqual("ABC", dogResultView);
			    }
		    }
	    }

	    public void EmptySequenceForDefaultProfile_TwoProfiles_Test()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .UseItems()
			    .AddNext<A>("Dog")
			    .AddNext<B>("Dog")
			    .AddNext<Y>("Dog", "Cat")
			    .AddNext<L>("Cat", "Dog")
			    .AddNext<M>("Cat")
			    .AddNext<R>("Cat")
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    CollectionAssert.IsEmpty(sequence);

			    using (var dogContainer = container.GetNestedContainer("Dog"))
			    {
				    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
				    var dogResultView = SelectResultViewFrom(dogSequence);
				    Assert.AreEqual("ABYL", dogResultView);
			    }

			    using (var catContainer = container.GetNestedContainer("Cat"))
			    {
				    var catSequence = catContainer.GetInstance<IEnumerable<I>>();
				    var catResultView = SelectResultViewFrom(catSequence);
				    Assert.AreEqual("YLMR", catResultView);
			    }
		    }
	    }

		[Test]
	    public void UseTwoNamedSequences_NoProfile_Test()
		{
		    var registry = new Registry();
			registry
				.ForSequenceOf<I>()
				.Named("Red")
				.UseItems()
				.AddNext<A>()
				.AddNext<B>()
				.AddNext<C>()
				.AddNext<D>()
				.AddNext<E>()
				.End();

		    registry
			    .ForSequenceOf<I>()
			    .Named("Blue")
			    .UseItems()
			    .AddNext<F>()
			    .AddNext<G>()
			    .AddNext<K>()
			    .AddNext<A>()
			    .AddNext<B>()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var redSequence = container.GetInstance<IEnumerable<I>>("Red");
			    var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
			    var defaultSequence = container.GetInstance<IEnumerable<I>>();

			    var redResultView = SelectResultViewFrom(redSequence);
			    var blueResultView = SelectResultViewFrom(blueSequence);
			    var defaultResultView = SelectResultViewFrom(defaultSequence);

				Assert.AreEqual("ABCDE", redResultView);
			    Assert.AreEqual("FGKAB", blueResultView);
			    Assert.AreEqual("FGKAB", defaultResultView);
			}
		}

	    [Test]
	    public void TwoNamedSequences_UseFirstAddSecond_NoProfile_Test()
		{
		    var registry = new Registry();
		    registry
			    .ForSequenceOf<I>()
			    .Named("Red")
			    .UseItems()
			    .AddNext<A>()
			    .AddNext<B>()
			    .AddNext<C>()
			    .AddNext<D>()
			    .AddNext<E>()
			    .End();

		    registry
			    .ForSequenceOf<I>()
			    .Named("Blue")
			    .AddItems()
			    .AddNext<F>()
			    .AddNext<G>()
			    .AddNext<K>()
			    .AddNext<A>()
			    .AddNext<B>()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var redSequence = container.GetInstance<IEnumerable<I>>("Red");
			    var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
			    var defaultSequence = container.GetInstance<IEnumerable<I>>();

			    var redResultView = SelectResultViewFrom(redSequence);
			    var blueResultView = SelectResultViewFrom(blueSequence);
			    var defaultResultView = SelectResultViewFrom(defaultSequence);

			    Assert.AreEqual("ABCDE", redResultView);
			    Assert.AreEqual("FGKAB", blueResultView);
			    Assert.AreEqual("ABCDE", defaultResultView);
		    }
	    }

	    [Test]
	    public void TwoNamedSequences_TwoProfiles_NotIntersectedItems_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForSequenceOf<I>()
			    .Named("Red")
			    .AddItems()
			    .AddNext<A>("Dog")
			    .AddNext<B>("Cat")
			    .AddNext<C>("Dog")
			    .AddNext<X>("Dog")
			    .AddNext<Y>("Cat")
			    .End();

		    registry
			    .ForSequenceOf<I>()
			    .Named("Blue")
			    .AddItems()
			    .AddNext<A>("Cat")
			    .AddNext<B>("Dog")
			    .AddNext<C>("Cat")
			    .AddNext<X>("Cat")
			    .AddNext<Y>("Dog")
			    .End();

		    using (var container = new Container(registry))
			using (var catContainer = container.GetNestedContainer("Cat"))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var redCatSequence = catContainer.GetInstance<IEnumerable<I>>("Red");
				var blueCatSequence = catContainer.GetInstance<IEnumerable<I>>("Blue");
				var redDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Red");
				var blueDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Blue");

				var redCatResultView = SelectResultViewFrom(redCatSequence);
				var blueCatResultView = SelectResultViewFrom(blueCatSequence);
				var redDogResultView = SelectResultViewFrom(redDogSequence);
				var blueDogResultView = SelectResultViewFrom(blueDogSequence);

				Assert.AreEqual("BY", redCatResultView);
				Assert.AreEqual("ACX", blueCatResultView);
				Assert.AreEqual("ACX", redDogResultView);
				Assert.AreEqual("BY", blueDogResultView);
			}
	    }

	    private string SelectResultViewFrom(IEnumerable<I> sequence) =>
		    string.Join(string.Empty, sequence.Select(o => o.Name));

		private interface I { string Name { get; } }
		private class A : I { public string Name => "A"; }
	    private class B : I { public string Name => "B"; }
		private class C : I { public string Name => "C"; }
		private class D : I { public string Name => "D"; }
		private class E : I { public string Name => "E"; }
		private class F : I { public string Name => "F"; }
		private class G : I { public string Name => "G"; }
		private class H : I { public string Name => "H"; }
		private class K : I { public string Name => "K"; }
		private class L : I { public string Name => "L"; }
		private class M : I { public string Name => "M"; }
		private class N : I { public string Name => "N"; }
		private class O : I { public string Name => "O"; }
		private class P : I { public string Name => "P"; }
		private class Q : I { public string Name => "Q"; }
		private class R : I { public string Name => "R"; }
		private class S : I { public string Name => "S"; }
		private class T : I { public string Name => "T"; }
		private class V : I { public string Name => "V"; }
		private class X : I { public string Name => "X"; }
		private class Y : I { public string Name => "Y"; }
		private class Z : I { public string Name => "Z"; }

	}
}
