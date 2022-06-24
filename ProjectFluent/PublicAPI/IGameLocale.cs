using System.Globalization;

namespace Shockah.ProjectFluent
{
	/// <summary>An instance representing a specific game locale, be it a built-in one or a mod-provided one.</summary>
	public interface IGameLocale
	{
		/// <summary>The locale code of this locale (for example, <c>en-US</c>).</summary>
		string LocaleCode { get; }

		/// <summary>The <see cref="System.Globalization.CultureInfo"/> for this locale.</summary>
		CultureInfo CultureInfo
			=> new(LocaleCode);

		/// <summary>Whether this locale is a built-in one.</summary>
		bool IsBuiltInLocale
			=> this is BuiltInGameLocale;

		/// <summary>Whether this locale is a mod-provided one.</summary>
		bool IsModLocale
			=> this is ModGameLocale;
	}
}