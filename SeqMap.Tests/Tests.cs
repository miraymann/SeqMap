using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureMap;

namespace SeqMap.Tests
{
    public class Tests
    {
		[Test]
	    public void NoProfileTest()
		{
			var registry = new Registry();
			registry.ForSequenceOf<I>()
				.Add<A>()
				.Add<D>()
				.Add<H>()
				.Add<G>()
				.Add<O>()
				.End();

			using (var container = new Container(registry))
			{
				var result = SelectResultViewFrom(container);
				Assert.AreEqual("ADHGO", result);
			}
		}

	    [Test]
	    public void ItemsDublication_NoProfileTest()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .Add<A>()
			    .Add<D>()
			    .Add<A>()
			    .Add<A>()
			    .Add<O>()
				.Add<C>()
			    .Add<C>()
			    .Add<C>()
				.End();

		    using (var container = new Container(registry))
		    {
			    var result = SelectResultViewFrom(container);
			    Assert.AreEqual("ADAAOCCC", result);
		    }
	    }

		[Test]
	    public void IsIndependentOfStandardStructureMap_NoProfileTest()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<Z>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<L>();

			registry.ForSequenceOf<I>()
			    .Add<A>()
			    .Add<D>()
			    .Add<H>()
			    .Add<G>()
			    .Add<O>()
			    .End();

		    registry.For<I>().Add<V>();
		    registry.For<I>().Add<T>();
		    registry.For<I>().Add<R>();

		    using (var container = new Container(registry))
		    {
			    var result = SelectResultViewFrom(container);
			    Assert.AreEqual("ADHGO", result);
		    }
	    }

	    [Test]
	    public void NoProfileSingleItemTest()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .Add<X>()
			    .End();

		    var container = new Container(registry);
		    var result = container.GetInstance<IEnumerable<I>>()
			    .ToArray();

		    Assert.AreEqual(1, result.Length);
		    Assert.AreEqual("X", result[0].Name);
		}

		[Test]
	    public void SingleProfileTest()
	    {
		    var registry = new Registry();
			registry.ForSequenceOf<I>()
			    .Add<A>()
			    .Add<D>()
			    .Add<H>("X")
			    .Add<G>()
			    .Add<O>("X")
				.Add<X>("X")
				.Add<Z>()
				.Add<E>()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var result = SelectResultViewFrom(container);
			    Assert.AreEqual("ADGZE", result);

			    using (var xContainer = container.GetNestedContainer("X"))
			    {
				    var xResult = SelectResultViewFrom(xContainer);
				    Assert.AreEqual("ADHGOXZE", xResult);
				}
		    }
	    }

		[Test]
	    public void MultiProfilesTest()
	    {
		    var registry = new Registry();
		    registry.ForSequenceOf<I>()
			    .Add<A>()
			    .Add<B>("A", "B", "C", "D", "E", "F", "G")
			    .Add<C>("A", "B", "C", "D", "E", "F")
			    .Add<G>("A", "B", "C", "D", "E")
			    .Add<O>("A", "B", "C", "D")
			    .Add<X>("A", "B", "C")
			    .Add<Z>("A", "B")
			    .Add<E>("A")
			    .End();

		    using (var container = new Container(registry))
		    {
			    var result = SelectResultViewFrom(container);
			    Assert.AreEqual("A", result);

			    var expectedResult = "ABCGOXZE";
				foreach (var profile in "ABCDEFG".Select(o => o.ToString()))
			    {
				    using (var profileContainer = container.GetNestedContainer(profile))
				    {
					    var profileResult = SelectResultViewFrom(profileContainer);
					    Assert.AreEqual(expectedResult, profileResult);
				    }

				    expectedResult = expectedResult
					    .Substring(0, expectedResult.Length - 1);
			    }
		    }
		}

	    private string SelectResultViewFrom(IContainer container) =>
		    string.Join(string.Empty, 
			    container.GetInstance<IEnumerable<I>>()
				         .Select(o => o.Name));

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
