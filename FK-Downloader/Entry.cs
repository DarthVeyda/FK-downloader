using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FK_Downloader
{
    class Entry
    {
        public static readonly List<string> TextHeadersAuthor = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Бета:</b>",
            "<b>Размер:</b>",
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", // (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Рейтинг:</b>",
            "<b>Краткое содержание:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> TextHeadersTranslated = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Переводчик:</b>",
            "<b>Бета:</b>",
            "<b>Оригинал:</b>",
            "<b>Ссылка на оригинал:</b>",
            "<b>Размер:</b>",
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", // (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Рейтинг:</b>",
            "<b>Краткое содержание:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> VisualHeadersArt = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",// (арт, клип, коллаж)
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", // (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Продолжительность и вес:</b>",
            "<b>Рейтинг:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> VisualHeadersCollage = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",// (арт, клип, коллаж)
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", // (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Исходники:</b>",
            "<b>Рейтинг:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> VisualHeadersVideo = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",// (арт, клип, коллаж)
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", // (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Исходники:</b>",
            "<b>Продолжительность и вес:</b>",
            "<b>Рейтинг:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> ChallengeHeadersComicStrip = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Перевод:</b>",
            "<b>Бета:</b>",
            "<b>Эдитор:</b>",
            "<b>Оригинал:</b>",
            "<b>Язык оригинала:</b>",
            "<b>Форма:</b>", //(додзинси, комикс, стрип)
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", //(джен, гет, слэш, фэмслэш)
            "<b>Рейтинг:</b>",
            "<b>Количество страниц:</b>",
            "<b>Краткое содержание:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> ChallengeHeadersCosplay = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Косплеер:</b>",
            "<b>Фотограф:</b>",
            "<b>Эдитор:</b>",
            "<b>Форма:</b>",// косплей
            "<b>Пейринг/Персонажи:</b>",
            "<b>Рейтинг:</b>",
            "<b>Количество:</b>",// (кол-во фотографий в сете)
            "<b>Примечание:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> ChallengeHeadersGeneral = new List<string>()
        {
            "<b>Название:</b>", //(если есть)
            "<b>Автор:</b>",
            "<b>Форма:</b>", //(сет аватарок, фанмикс, тест и так далее)
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>", //(джен, гет, слэш, фэмслэш)
            "<b>Рейтинг:</b>",
            "<b>Исходники:</b>", //(музыкальная композиция, если это перепетая песня, офарты/чужие арты, если это дизайн с их использованием - в общем, все, что можно закопирайтить)
            "<b>Размер:</b>",//(кол-во слов для текстовой формы; количество штук для сета аватарок или фанмикса)
            "<b>Продолжительность и вес:</b>", //(если это музыка, клипы, и если есть ссылка на скачиваемые файлы)
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> BigBangHeadersTextAuthor = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Бета:</b>",
            "<b>Размер:</b>",
            "<b>Пейринг/Персонажи:",
            "<b>Категория:</b>",// (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Рейтинг:</b>",
            "<b>Краткое содержание:</b>",
            "<b>Иллюстрация:</b>",
            "<b>Примечание/Предупреждения:</b> ",
            "<b>Для голосования:</b> "
        };

        public static readonly List<string> BigBangHeadersTextTranslated = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Переводчик:</b>",
            "<b>Бета:</b>",
            "<b>Оригинал:</b>",
            "<b>Ссылка на оригинал:</b>",
            "<b>Размер:</b>",
            "<b>Пейринг/Персонажи:",
            "<b>Категория:</b>",// (джен, гет, слэш, фэмслэш)
            "<b>Жанр:</b>",
            "<b>Рейтинг:</b>",
            "<b>Краткое содержание:</b>",
            "<b>Иллюстрация:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b> "
        };

        public static readonly List<string> BigBangHeadersTextIllustrArt = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>",
            "<b>Жанр:</b>",
            "<b>Рейтинг:</b>",
            "<b>Иллюстрация к макси:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> BigBangHeadersTextIllustrCollage = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>",
            "<b>Жанр:</b>",
            "<b>Исходники:</b>",
            "<b>Рейтинг:</b>",
            "<b>Иллюстрация к макси:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public static readonly List<string> BigBangHeadersTextIllustrVideo = new List<string>()
        {
            "<b>Название:</b>",
            "<b>Автор:</b>",
            "<b>Форма:</b>",
            "<b>Пейринг/Персонажи:</b>",
            "<b>Категория:</b>",
            "<b>Жанр:</b>",
            "<b>Исходники:</b>",
            "<b>Продолжительность и вес:</b>",
            "<b>Рейтинг:</b>",
            "<b>Иллюстрация к макси:</b>",
            "<b>Примечание/Предупреждения:</b>",
            "<b>Для голосования:</b>"
        };

        public FileWithProperties MetaProperties { get; private set; }

        public EntryType Type { get; private set; }

        public string Name { get; private set; }

        public string CycleName { get; private set; }

        public string Form { get; private set; }

        public string Category { get; private set; }

        public StringBuilder TextContent { get; private set; }

        public List<string> ImageURLs { get; private set; }

        public List<string> VideoURLs { get; private set; }

        public Entry(FileWithProperties fileProperties)
        {
            MetaProperties = fileProperties;
        }

        public void ParseHeader(string rawHeader)
        {

        }

    }

    enum EntryType
    {
        Text = 1,
        Art,
        Collage,
        Video,
        Analytics,
        Fanmix,
        Avatars,
        Cooking,
        Handmade,
        Minipies
    }
}
