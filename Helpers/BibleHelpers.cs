using System;
using System.Linq;
using System.Text;

namespace ProverbTeleprompter.Helpers
{
    public class BibleHelpers
    {
        private static readonly string[] OldTestamentBooks = new[]
                                                     {
                                                         "Genesis",
                                                         "Exodus",
                                                         "Leviticus",
                                                         "Numbers",
                                                         "Deuteronomy",
                                                         "Joshua",
                                                         "Judges",
                                                         "Ruth",
                                                         "1 Samuel",
                                                         "2 Samuel",
                                                         "1 Kings",
                                                         "2 Kings",
                                                         "1 Chronicles",
                                                         "2 Chronicles",
                                                         "Ezra",
                                                         "Nehemiah",
                                                         "Esther",
                                                         "Job",
                                                         "Psalm",
                                                         "Proverbs",
                                                         "Ecclesiastes",
                                                         "Song of Solomon",
                                                         "Isaiah",
                                                         "Jeremiah",
                                                         "Lamentations",
                                                         "Ezekiel",
                                                         "Daniel",
                                                         "Hosea",
                                                         "Joel",
                                                         "Amos",
                                                         "Obadiah",
                                                         "Jonah",
                                                         "Micah",
                                                         "Nahum",
                                                         "Habakkuk",
                                                         "Zephaniah",
                                                         "Haggai",
                                                         "Zechariah",
                                                         "Malachi",

                                                     };

        private static readonly string[] NewTestamentBooks = new[]
                                                                  {
                                                                      "Matthew",
                                                                      "Mark",
                                                                      "Luke",
                                                                      "John",
                                                                      "Acts",
                                                                      "Romans",
                                                                      "1 Corinthians",
                                                                      "2 Corinthians",
                                                                      "Galatians",
                                                                      "Ephesians",
                                                                      "Philippians",
                                                                      "Colossians",
                                                                      "1 Thessalonians",
                                                                      "2 Thessalonians",
                                                                      "1 Timothy",
                                                                      "2 Timothy",
                                                                      "Titus",
                                                                      "Philemon",
                                                                      "Hebrews",
                                                                      "James",
                                                                      "1 Peter",
                                                                      "2 Peter",
                                                                      "1 John",
                                                                      "2 John",
                                                                      "3 John",
                                                                      "Jude",
                                                                      "Revelation"
                                                                  };

        internal static BiblePassageInfo  GetRandomBook()
        {
            Random r = new Random();
            BiblePassageInfo info = new BiblePassageInfo();
            if(r.Next(0,2) == 1)
            {
                info.BibleCode = "kjv";
                info.Book = OldTestamentBooks[r.Next(0, OldTestamentBooks.Count())];
            }
            else
            {
                info.BibleCode = "leb";
                info.Book =  NewTestamentBooks[r.Next(0, NewTestamentBooks.Count())];
            }


            return info;
        }

        internal static string GetRandomBibleChapterHtml()
        {

            //StringBuilder url = new StringBuilder("http://labs.bible.org/api/?passage=");
            string bibliaKey = "cdc31adc54172fbd4e07bfda6bf3f931";
            var info = BibleHelpers.GetRandomBook();
            StringBuilder url = new StringBuilder("http://api.biblia.com/v1/bible/content/");
            url.AppendFormat("{2}.html?passage={1}&key={0}&fullText=true", bibliaKey, info.Book, info.BibleCode);


            //Load a random chapter from the bible
            return WebHelpers.GetUrlContent(new Uri(url.ToString()));

        }
    }


    public class BiblePassageInfo
    {
        public string BibleCode { get; set; }

        public string Book { get; set; }
    }
}
