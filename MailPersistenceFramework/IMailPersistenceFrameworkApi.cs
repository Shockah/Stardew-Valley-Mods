using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.MailPersistenceFramework
{
	public enum MailBackground
	{
		Classic = 0,
		Notepad = 1,
		Pyramids = 2
	}

	public enum MailTextColor
	{
		DarkRed = -1,
		Black = 0,
		SkyBlue = 1,
		Red = 2,
		BlueViolet = 3,
		White = 4,
		OrangeRed = 5,
		LimeGreen = 6,
		Cyan = 7,
		DarkestGray = 8
	}
	
	public enum MailApiAttribute
	{
		/// <summary>
		/// Custom tags for a given mail, which can be used to determine the mail type and override some of its attributes at runtime.
		/// </summary>
		/// <remarks>
		/// The value is of type <see cref="IReadOnlyDictionary{TKey, TValue}"/> (where both <c>TKey</c> and <c>TValue</c> are <see cref="string"/>).<br/>
		/// On input, <see cref="object"/> type is allowed and will be converted into <see cref="IReadOnlyDictionary{string, string}"/> (where both <c>TKey</c> and <c>TValue</c> are <see cref="string"/>).
		/// </remarks>
		Tags,

		/// <summary>Text of the given mail.</summary>
		/// <remarks>The value is of type <see cref="string"/>.</remarks>
		Text,

		/// <summary>The items attached to the mail.</summary>
		/// <remarks>
		/// The value is of type <see cref="IReadOnlyList{T}"/> (where <c>T</c> is <see cref="Item"/>).<br/>
		/// On input, <see cref="IEnumerable{T}"/> type (where <c>T</c> is <see cref="Item"/>) is allowed and will be converted.<br/>
		/// On input, <see cref="Item"/> type is allowed and will be wrapped.
		/// </remarks>
		Items,

		/// <summary>A recipe name attached to the mail.</summary>
		/// <remarks>The value is of type <see cref="string"/>.</remarks>
		Recipe,

		/// <summary>The background ID to use for this mail.</summary>
		/// <remarks>
		/// The value is of type <see cref="int"/>.<br/>
		/// On input, <see cref="MailBackground"/> type is allowed and will be casted.<br/>
		/// See <see cref="MailBackground"/> for allowed values.
		/// </remarks>
		Background,

		/// <summary>The text color ID to use for this mail.</summary>
		/// <remarks>
		/// The value is of type <see cref="int"/>.<br/>
		/// On input, <see cref="MailTextColor"/> type is allowed and will be casted.<br/>
		/// See <see cref="MailTextColor"/> for allowed values.
		/// </remarks>
		TextColor,
	}

	public interface IMailPersistenceFrameworkApi
	{
		/// <summary>
		/// Registers mod overrides for mails.
		/// </summary>
		/// <param name="mod">The mod's manifest.</param>
		/// <param name="text">
		/// The text override.<br/>
		/// Parameter #1 (<see cref="string"/>): <c>modUniqueID</c> - The mod's unique ID.<br/>
		/// Parameter #2 (<see cref="string"/>): <c>mailID</c> - The mail's ID.<br/>
		/// Parameter #3 (<see cref="string"/>): <c>text</c> - The mail's current text.<br/>
		/// Parameter #4 (<see cref="Action{T}"/> (where <c>T</c> is <see cref="string"/>)): <c>@override</c> - A delegate to call to override the value.<br/>
		/// </param>
		/// <param name="items">
		/// The attached items override.<br/>
		/// Parameter #1 (<see cref="string"/>): <c>modUniqueID</c> - The mod's unique ID.<br/>
		/// Parameter #2 (<see cref="string"/>): <c>mailID</c> - The mail's ID.<br/>
		/// Parameter #3 (<see cref="IReadOnlyList{T}"/> (where <c>T</c> is <see cref="Item"/>)): <c>items</c> - The mail's current attached items.<br/>
		/// Parameter #4 (<see cref="Action{T}"/> (where <c>T</c> is <see cref="IEnumerable{T}"/> (where <c>T</c> is <see cref="Item"/>))): <c>@override</c> - A delegate to call to override the value.<br/>
		/// </param>
		/// <param name="recipe">
		/// The attached recipe name override.<br/>
		/// Parameter #1 (<see cref="string"/>): <c>modUniqueID</c> - The mod's unique ID.<br/>
		/// Parameter #2 (<see cref="string"/>): <c>mailID</c> - The mail's ID.<br/>
		/// Parameter #3 (<see cref="string"/>): <c>recipe</c> - The mail's current attached recipe name.<br/>
		/// Parameter #4 (<see cref="Action{T}"/> (where <c>T</c> is <see cref="string"/>?)): <c>@override</c> - A delegate to call to override the value.<br/>
		/// </param>
		void RegisterModOverrides(
			IManifest mod,
			Action<string, string, string, Action<string>>? text = null,
			Action<string, string, IReadOnlyList<Item>, Action<IEnumerable<Item>>>? items = null,
			Action<string, string, string?, Action<string?>>? recipe = null
		);

		/// <summary>
		/// Sends a mail to the specified player.
		/// </summary>
		/// <param name="mod">The mod's manifest.</param>
		/// <param name="addressee">The player to the send the mail to.</param>
		/// <param name="attributes">The mail's <see cref="MailApiAttribute">attributes</see>.</param>
		/// <returns>The created mail's ID, which can be used with other methods of this API.</returns>
		string SendMail(IManifest mod, Farmer addressee, IReadOnlyDictionary<int /* MailApiAttribute */, object?> attributes);

		/// <summary>
		/// Sends a mail to the local player.
		/// </summary>
		/// <param name="mod">The mod's manifest.</param>
		/// <param name="attributes">The mail's <see cref="MailApiAttribute">attributes</see>.</param>
		/// <returns>The created mail's ID, which can be used with other methods of this API.</returns>
		string SendMailToLocalPlayer(IManifest mod, IReadOnlyDictionary<int /* MailApiAttribute */, object?> attributes);

		/// <summary>
		/// Gets the IDs of all of the existing mails sent for this mod.
		/// </summary>
		/// <param name="modUniqueID">The mod's unique ID.</param>
		/// <returns>An <see cref="IEnumerable{string}"/> of existing mails.</returns>
		IEnumerable<string> GetMailIDs(string modUniqueID);

		/// <summary>
		/// Tests if a given mail exists.
		/// </summary>
		/// <param name="modUniqueID">The mod's unique ID.</param>
		/// <param name="mailID">The mail's ID.</param>
		/// <returns>Whether a given mail exists.</returns>
		bool HasMail(string modUniqueID, string mailID);

		/// <summary>
		/// Gets the <see cref="Farmer"/> address of a mail.
		/// </summary>
		/// <param name="modUniqueID">The mod's unique ID.</param>
		/// <param name="mailID">The mail's ID.</param>
		/// <returns>The <see cref="Farmer"/> addressee of the mail.</returns>
		/// <exception cref="ArgumentException">When a given mail does not exist.</exception>
		Farmer GetMailAddressee(string modUniqueID, string mailID);

		/// <summary>
		/// Gets the mail's tags.
		/// </summary>
		/// <param name="modUniqueID">The mod's unique ID.</param>
		/// <param name="mailID">The mail's ID.</param>
		/// <returns>The mail's tags.</returns>
		/// <exception cref="ArgumentException">When a given mail does not exist.</exception>
		IReadOnlyDictionary<string, string> GetMailTags(string modUniqueID, string mailID);

		/// <summary>
		/// Gets the attributes of a mail.
		/// </summary>
		/// <param name="modUniqueID">The mod's uniqueID.</param>
		/// <param name="mailID">The mail's ID.</param>
		/// <returns>The attributes of the mail.</returns>
		/// <exception cref="ArgumentException">When a given mail does not exist.</exception>
		IReadOnlyDictionary<int /* MailApiAttribute */, object?> GetMailAttributes(string modUniqueID, string mailID);
	}
}
