//=========================================================================
///	<summary>
///		AnimeProgramリストのソートコンペアクラス
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2006/XX/XX 新規作成</history>
//=========================================================================
using System;
using System.Collections.Generic;
using System.Text;
using magicAnime.Properties;

namespace magicAnime
{
    //=========================================================================
    ///	<summary>
    ///		AnimeProgramリストのソートコンペアラクラス
    ///	</summary>
    /// <remarks>
    /// </remarks>
    /// <history>2006/XX/XX 新規作成</history>
    //=========================================================================
    // mod yossiepon 20191124 begin
    //class AnimeSort : System.Collections.Generic.IComparer<AnimeProgram>
    abstract class AnimeSort : System.Collections.Generic.IComparer<AnimeProgram>
    // mod yossiepon 20191124 end
    {
        //--------------------
        // コンペア条件
        //--------------------

        public enum Order
		{
			DayOfWeek,		// 曜日ごと
			NextOnair,		// 次回放送日時順
			//			ReverseDayOfWeek,		// 曜日ごと(逆順)
		};

		[FlagsAttribute]
		public enum OrderOption
		{
			LastOrder = 1,// 放送終了を末尾に
			Limit1CoursOption = 2,// ソート基準を最新1クールに限る
		};

        // mod yossiepon 20191124 begin
        //private Order order;
		//private OrderOption orderOption;
        ////		private bool last;
        protected Order order;
        protected OrderOption orderOption;
        // mod yossiepon 20191124 end

        //=========================================================================
        ///	<summary>
        ///		コンストラクタ
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2006/XX/XX 新規作成</history>
        //=========================================================================
        public AnimeSort( Order order, OrderOption option )
		{
			this.order = order;
			this.orderOption = option;
			//			this.last			= last;
		}

        // add yossiepon 20191124 begin
        public abstract void PreScan(AnimeProgram p, DateTime baseTime);
        // add yossiepon 20191124 end

        //=========================================================================
        ///	<summary>
        ///		AnimeProgramの放送時間コンペア
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2006/XX/XX 新規作成</history>
        //=========================================================================
        // mod yossiepon 20191124 begin
        public abstract int Compare(AnimeProgram x, AnimeProgram y);
#if false
        public int Compare(AnimeProgram x, AnimeProgram y)
        {
            AnimeEpisode nextA, nextB;
			AnimeProgram.NextEpisode resultA, resultB;

			//--------------------------
			// 話数が0の場合は前に回す
			//--------------------------
            // mod yossiepon 20160808 begin
            //if ((x.StoryCount == 0) && (y.StoryCount == 0))
            //    return 0;
            //if ((x.StoryCount != 0) && (y.StoryCount == 0))
            //    return +1;
            //if ((x.StoryCount == 0) && (y.StoryCount != 0))
            //    return -1;
            if ( ((x.StoryCount + x.SpecialStoryCount) == 0) && ((y.StoryCount + y.SpecialStoryCount) == 0) )
				return 0;
			if ( ((x.StoryCount + x.SpecialStoryCount) != 0) && ((y.StoryCount + y.SpecialStoryCount) == 0) )
				return +1;
			if ( ((x.StoryCount + x.SpecialStoryCount) == 0) && ((y.StoryCount + y.SpecialStoryCount) != 0) )
				return -1;
            // mod yossiepon 20160808 end

			//--------------------------
			// 放送終了分を末尾に回す
			//--------------------------

			DateTime now = DateTime.Now;

			resultA = x.GetNextEpisode( now, out nextA );
			resultB = y.GetNextEpisode( now, out nextB );

			if( ((orderOption & OrderOption.LastOrder) != 0) ||
				  order == Order.NextOnair ) // 次回放送日順の場合は強制
			{
				if( (resultA != AnimeProgram.NextEpisode.NextDecided) &&
					(resultB != AnimeProgram.NextEpisode.NextDecided) )
				{
					return 0;
				}
				if( (resultA != AnimeProgram.NextEpisode.NextDecided) &&
					 (resultB == AnimeProgram.NextEpisode.NextDecided) )
				{
					return +1;
				}
				if( (resultA == AnimeProgram.NextEpisode.NextDecided) &&
					(resultB != AnimeProgram.NextEpisode.NextDecided) )
				{
					return -1;
				}
			}

			if( order == Order.DayOfWeek )
			{
				//------------------------
				// 放送曜日順
				//------------------------
				int aCount, bCount;
				long aMinute = 0, bMinute = 0;

				//------------------------------------
				// 各話の週開始からの平均時分を計算
				//------------------------------------

				aCount = 0;
				// mod yossiepon 20160924 begin
				// foreach( AnimeEpisode aRecord in x.Episodes )
				foreach( AnimeEpisode aRecord in x.NormalEpisodes )
				// mod yossiepon 20160924 end
				{
					if( (orderOption & OrderOption.Limit1CoursOption) != 0 )		// 最新1クールに限るオプション(070612)
					{
                        // mod yossiepon 20160808 begin
                        //if (aRecord.StoryNumber < x.StoryCount - 13)
                        //    continue;
                        if (x.StoryCount >= 13)
                        {
                            if (aRecord.StoryNumber < x.StoryCount - 13)
								continue;
						}
                        // mod yossiepon 20160808 end
                    }
                    // <MOD> 2009/12/28 ->
                    if( aRecord.HasPlan )
//					if( aRecord.CurrentState != AnimeEpisode.State.Undecided )
// <MOD> 2009/12/28 <-
					{
// <MOD> 2009/12/28 ->
						DateTime convTime = aRecord.StartDateTime.AddHours( -(Settings.Default.hoursPerDay - 24) );
//						DateTime convTime = aRecord.StartDateTime.Value.AddHours( -(Settings.Default.hoursPerDay - 24) );
// <MOD> 2009/12/28 <-
						aMinute += ((long)convTime.DayOfWeek * 24 + convTime.Hour) * 60 + convTime.Minute;
						++aCount;
					}
				}

				bCount = 0;
				// mod yossiepon 20160924 begin
				// foreach( AnimeEpisode bRecord in y.Episodes )
				foreach( AnimeEpisode bRecord in y.NormalEpisodes )
				// mod yossiepon 20160924 end
				{
					if( (orderOption & OrderOption.Limit1CoursOption) != 0 )		// 最新1クールに限るオプション(070612)
					{
                        // mod yossiepon 20160808 begin
                        //if (bRecord.StoryNumber < y.StoryCount - 13)
                        //    continue;
                        if (y.StoryCount >= 13)
                        {
                            if (bRecord.StoryNumber < y.StoryCount - 13)
								continue;
						}
                        // mod yossiepon 20160808 end
                    }
// <MOD> 2009/12/28 ->
					if( bRecord.HasPlan )
//					if( bRecord.CurrentState != AnimeEpisode.State.Undecided )
// <MOD> 2009/12/28 <-
					{
// <MOD> 2009/12/28 ->
						DateTime convTime = bRecord.StartDateTime.AddHours( -(Settings.Default.hoursPerDay - 24) );
//						DateTime convTime = bRecord.StartDateTime.Value.AddHours( -(Settings.Default.hoursPerDay - 24) );
// <MOD> 2009/12/28 <-
						bMinute += ((long)convTime.DayOfWeek * 24 + convTime.Hour) * 60 + convTime.Minute;
						++bCount;
					}
				}

				// 070613
				if( (aCount == 0) && (bCount == 0) )
					return 0;
				if( (aCount == 0) && (bCount != 0) )
					return +1;
				if( (aCount != 0) && (bCount == 0) )
					return -1;

				aMinute /= aCount;
				bMinute /= bCount;

				if( aMinute < bMinute )
					return -1;
				else if( bMinute < aMinute )
					return +1;
				else
					return 0;
			}
			else if( order == Order.NextOnair )	
			{
				//------------------------
				// 次回放送順
				//------------------------

				if( nextA.StartDateTime == nextB.StartDateTime )
					return 0;
				if( nextA.StartDateTime < nextB.StartDateTime )
					return -1;
				else
					return +1;
			}

            return 0;
		}
#endif
// mod yossiepon 20191124 end
    }
}
