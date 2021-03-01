# TranslationToolsHS2
  Translations Tools for Honey Select 2
  
## MachineTranslate

This simple tool reads the untranslated text from a given folder, translate them with online machine services, perform style checks and make a file with all translations.

HOW TO USE
Open the file "MachineTranslate.exe" and enter the desired folder containing untranslated text. The result will be in the file "MachineTranslation\MachineTranslationsFinal.txt".
There are more files in this folder, they are used to resume the translation process if the tool was closed for some reason.

CONFIGURATION FILES
1) Languages.txt: Set the original and destination languages using the GoogleTranslate Languages Codes in the format "Original:Destination". Example: ja:en
2) Retranslate.txt: All machine translation sites are prone to errors. This file set the rules to detect severe mistranslations that are unrecoverable, like when a word is not translated at all. It uses Regular Expressions (REGEX). All lines that match these rules will be retranslated with BingTranslator.
3) Substitutions.txt: Substitute common errors with the format: oldWord=newWord
   Regular Expressions can be used with the format: r:"regex expression"="substitution" (quotes needed)

HOW IT WORKS
1) Reads all untranslated lines in .txt files from the given source folder and its subfolders. 
   Are considered as untranslated lines starting with "//" and having an "=" sign. Example: \\寝起き=
2) Reads the translated lines from both the source folder and the destination folder "MachineTranslation".
   Are considered as translated lines without "//" in the beginning and with some text after the "=" sign. Example: 寝起き=Wake up
3) Translate all untranslated lines that don't have a translation in both source and destination folder using GoogleTranslate.
   Translated lines are saved in "MachineTranslation\1-GoogleTranslateRAW.txt".   
4) Detects errored translations using Retranslate.txt and retranslate using BingTranslator
   Translated lines are saved in "MachineTranslation\2-BingTranslateRAW.txt".
5) Substitutes all strings using Substitutions.txt. (useful for style check)
6) The final file is saved as "MachineTranslation\MachineTranslationsFinal.txt".

## ReleaseToolHS2

  Read the translations repository folder and make a file clean to release. The release file will have:
  
  1) Cleaned all empty and commented lines in RedirectedResources (if it exist). If a file only has empty or commented lines it will be ignored.
  
  2) Folders RedirectedResources, Text and Texture will be zipped (if they exist).
  
  3) readme, license and config folder.
  
  **IMPORTANT:** The file `config\ AutoTranslatorConfig.ini` must have the Language key configured correctly.
  
  **v1 - [Download](https://github.com/SpockBauru/TranslationToolsHS2/releases/tag/r3)**

## Translate Duplicates

  Searches an entire folder if an untranslated line has been translated in other place and writes that translation. If there are several translations for the same sentence, only the first one is used. Useful for RedirectedResources folder. Use with caution!
  
  **v1 - [Download](https://github.com/SpockBauru/TranslationToolsHS2/releases/tag/r2)**

## Delete Duplicates

  Create a new file without duplicated lines. Its a way faster than Notepad++  
  
  **v1 - [Download](https://github.com/SpockBauru/TranslationToolsHS2/releases/tag/r1)**
