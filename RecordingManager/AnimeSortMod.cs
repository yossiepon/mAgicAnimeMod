//=========================================================================
///	<summary>
///		AnimeProgramリストのソートコンペアクラス
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2019/11/24 新規作成</history>
//=========================================================================
using System;
using System.Collections.Generic;
using magicAnime.Properties;

namespace magicAnime
{
    //=========================================================================
    ///	<summary>
    ///		AnimeProgramリストのソートコンペアラクラス
    ///	</summary>
    /// <remarks>
    /// </remarks>
    /// <history>2019/11/24 新規作成</history>
    //=========================================================================
    class AnimeSortMod : AnimeSort
    {
        private const int SECONDS_OF_DAY = 86400;
        private const int SECONDS_OF_HOUR = 3600;
        private const int HOURS_OF_DAY = 24;
        private const int DAYS_OF_WEEK = 7;

        private const long TICKS_OF_SECOND = 10000000;

        //=========================================================================
        ///	<summary>
        ///		コンストラクタ
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 新規作成</history>
        //=========================================================================
        public AnimeSortMod(Order order, OrderOption option) : base(order, option)
        {
        }

        //=========================================================================
        ///	<summary>
        ///		AnimeProgramを前処理スキャンしてソート代表値を決定する
        ///	</summary>
        ///	<param name="p">番組</param>
        ///	<param name="baseTime">ソート基準日時</param>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 新規作成</history>
        //=========================================================================
        public override void PreScan(AnimeProgram p, DateTime baseTime)
        {
            p.mSortRepresentative = DateTime.MaxValue;

            // 話数が0の場合は前に回す
            if (p.StoryCount == 0)
            {
                p.mSortRepresentative = DateTime.MinValue;
                return;
            }

            switch (order)
            {
                // 曜日と時刻の平均を基準にソート
                case Order.DayOfWeek:
                    PreScanDayOfWeek(p, baseTime);
                    break;
                // 次回の放送日時でソート
                default:
                case Order.NextOnair:
                    PreScanNextOnAir(p, baseTime);
                    break;
            }
        }

        private void PreScanDayOfWeek(AnimeProgram p, DateTime baseTime)
        {
            //--------------------------
            // ソート基準日時以降の放送回を取得
            //（放送開始日時が基準日時以降であれば含める）
            //--------------------------
            List<AnimeEpisode> normals = p.Episodes.FindAll(e => e.HasPlan && e.StartDateTime >= baseTime);

            // 通常回が存在すれば
            if (normals.Count > 0)
            {
                // 最新13話のみで判断する場合
                if ((orderOption & OrderOption.Limit1CoursOption) != 0)
                {
                    // 通常回が14話以上あれば先頭の余分な放送回を削除
                    if (normals.Count > 13)
                    {
                        normals.Sort(comp);
                        normals.RemoveRange(0, normals.Count - 13);
                    }
                }

                // 通常回のみで曜日と時刻の平均をとる
                long ticks = 0;
                normals.ForEach(e => ticks += ModuloDateTimeOfWeek(e));
                p.mSortRepresentative = new DateTime(ticks / normals.Count * TICKS_OF_SECOND);
            }
            // 放送終了分を末尾に回す
            else
            {
                AnimeEpisode last = LastEpicode(p);

                // 放送終了分を末尾に回す
                if ((orderOption & OrderOption.LastOrder) != 0)
                {
                    // 放送終了分を一番最後に放送された順番で末尾に並べる
                    p.mSortRepresentative = last.StartDateTime;
                    return;
                }
                // 一番最後に放送された曜日と時刻で並べる
                else
                {
                    p.mSortRepresentative = new DateTime(ModuloDateTimeOfWeek(last) * TICKS_OF_SECOND);
                }
            }
        }

        private void PreScanNextOnAir(AnimeProgram p, DateTime baseTime)
        {
            // 次の放送回を検索する
            AnimeEpisode ep;
            AnimeProgram.NextEpisode epType = p.GetNextEpisode(baseTime, out ep);

            // 次の放送回が存在しなければ
            if (epType != AnimeProgram.NextEpisode.NextDecided)
            {
                // 一番最後に放送された放送回を選択する
                ep = LastEpicode(p);

                // 放送終了分を末尾に回す
                if ((orderOption & OrderOption.LastOrder) != 0)
                {
                    // 放送終了分を一番最後に放送された順番で末尾に並べる
                    // 放送開始日時に100年を足したものを代表値とする
                    p.mSortRepresentative = ep.StartDateTime.AddYears(100);
                    return;
                }
            }

            // 取得した放送回の放送開始日時をソート代表値とする
            p.mSortRepresentative = ep.StartDateTime;
        }

        private static long ModuloDateTimeOfWeek(AnimeEpisode e)
        {
            long shiftDayOfWeek = 1;
            return (e.StartDateTime.Ticks / TICKS_OF_SECOND
                    - (Settings.Default.hoursPerDay - HOURS_OF_DAY) * SECONDS_OF_HOUR + SECONDS_OF_DAY * shiftDayOfWeek)
                            % (SECONDS_OF_DAY * DAYS_OF_WEEK);
        }

        private static AnimeEpisode LastEpicode(AnimeProgram p)
        {
            List<AnimeEpisode> episodes = p.Episodes;
            episodes.Sort(comp);

            return episodes[episodes.Count - 1];
        }

        private static Comparison<AnimeEpisode> comp = (x, y) => x.StartDateTime.CompareTo(y.StartDateTime);

        //=========================================================================
        ///	<summary>
        ///		AnimeProgramの放送時間コンペア
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 新規作成</history>
        //=========================================================================
        public override int Compare(AnimeProgram x, AnimeProgram y)
        {
            return x.mSortRepresentative.CompareTo(y.mSortRepresentative);
        }
    }
}
