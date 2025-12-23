HOW TO USE THIS TEMPLATE
=========================

1. COPY this entire "_TEMPLATE" folder
2. RENAME the copy to your language code (2 letters, UPPERCASE)
   Examples: FR, DE, ES, IT, PL, RU, TR, BR, JP, KO

3. EDIT language_data.xml:
   - Change "YourLanguage" to your language name (in your language)
     Example: "Français", "Deutsch", "Español"
   - Change "XX" to your 2-letter code (lowercase)
   - Change "supported_iso" to your language's ISO codes
     Find codes here: https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes
   - Change "under_development" to "false" when translation is complete

4. RENAME enlisted_strings_template.xml to enlisted_strings_XX.xml
   (XX = your 2-letter language code, lowercase)

5. COPY the English enlisted_strings.xml content into your renamed file

6. TRANSLATE the text="" attributes
   - DO translate: text="Status Report"
   - DON'T change: id="Enlisted_Menu_Status_Title"
   - DON'T translate: {PLAYER_NAME}, {RANK}, etc.

7. TEST in-game:
   - Launch Bannerlord
   - Options → Gameplay → Language
   - Select your language
   - Restart game

Your folder structure should look like:
Languages/
  ├─ FR/  (or your language code)
  │   ├─ language_data.xml
  │   └─ enlisted_strings_fr.xml

COMMON LANGUAGE CODES:
FR = French (Français)
DE = German (Deutsch)
ES = Spanish (Español)
IT = Italian (Italiano)
PL = Polish (Polski)
RU = Russian (Русский)
TR = Turkish (Türkçe)
BR = Portuguese (Português)
JP = Japanese (日本語)
KO = Korean (한국어)
CNs = Chinese Simplified (简体中文)
CNt = Chinese Traditional (繁體中文)

Need help? Check the main README.md in Languages/ folder!

