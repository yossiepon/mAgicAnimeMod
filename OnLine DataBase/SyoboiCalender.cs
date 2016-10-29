//=========================================================================
///	<summary>
///		しょぼいカレンダー オンラインデータ取得クラス
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2006/XX/XX 新規作成 Dr.Kurusugawa</history>
/// <history>2010/05/01 Subversionで管理するため不要なコメント削除</history>
//=========================================================================
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml;
using System.IO;
using System.Collections;
using System.Web;
using  System.Windows.Forms;
using magicAnime.Properties;

namespace magicAnime
{
	//=========================================================================
	///	<summary>
	///		しょぼいカレンダーデータ取得クラス
	///	</summary>
	/// <remarks>
	/// </remarks>
	/// <history>2006/XX/XX 新規作成</history>
	//=========================================================================
	class SyoboiCalender
	{
		// add yossiepon 20150705 begin
		public const int UNNUMBERED_EPISODE = int.MinValue;
        // add yossiepon 20150705 end

		DateTime?	prevUpdateListGetTime	= null;			// 前回の更新リスト取得時刻

		// タイトルリストテーブル(http://cal.syoboi.jp/titlelist.php)
		// タイトル	初回放送	初回終了	Point	TID	更新
		
		//=========================================================================
		///	<summary>
		///		しょぼかるの番組データを保持するクラス
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		public class SyoboiProgram
		{
			public string title;
			public 	struct SeasonOnAir
			{
				public const	uint YearDecided		= 1;
				public const	uint MonthDecided		= 2;
				public uint	RecordState;
				public int		year;
				public int		month;
				public override string ToString()
				{
					return string.Format(
						"{0:0}-{1:0}",
						((RecordState&YearDecided)>0)?year.ToString():"????",
						((RecordState&MonthDecided)>0)?month.ToString():"??");
				}
			};
			public SeasonOnAir seasonOnAir;
			
			public int	tid;
			
		}

		//=========================================================================
		///	<summary>
		///		しょぼかるのエピソードデータを保持するクラス
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		public class SyoboiRecord : ICloneable
		{
			// add yossiepon 20150705 begin
			public string episode;
			// add yossiepon 20150705 end
			public int number;
			public string subtitle;
			public string tvStation;
			public int length;
			
			public DateTime onAirDateTime;

			public System.Object Clone()
			{
				return this.MemberwiseClone();
			}

			// add yossiepon 20150705 begin
			//=========================================================================
			///	<summary>
			///		フォーマットされた番組エピソード文字列を返す
			///	</summary>
			/// <remarks>
			/// </remarks>
			/// <history>2006/XX/XX 新規作成</history>
			//=========================================================================
			public override string ToString()
			{
				return tvStation + "-No." + number + "(" + episode + ")「" + subtitle + "」(" + length + " min.)/" + onAirDateTime;
			}
			// add yossiepon 20150705 end
		}

		public class SyoboiUpdate
		{
			public int tid;
			public DateTime updateDate;
		}
		
		//=========================================================================
		///	<summary>
		///		mAgicAnime固有のUser-Agent文字列を返す
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2007/12/22 新規作成</history>
		//=========================================================================
		public string UserAgent
		{
			get
			{
				return "mAgicAnime " + Application.ProductVersion.ToString();
			}
		}

		//=========================================================================
		///	<summary>
		///		しょぼかるRSSから更新された番組リストを取得
		///	</summary>
		/// <remarks>
		///		更新がない場合は空のリストを返す。
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		/// <history>2009/02/11 しょぼかる負荷対策のためif-modified-since追加</history>
		//=========================================================================
		public List<SyoboiUpdate> DownloadUpdateList()
		{
			List<SyoboiUpdate> updateList = new List<SyoboiUpdate>();

			try
			{
				//------------------------
				// しょぼかるRSSを開く
				//------------------------
				HttpWebRequest webRequest =
					HttpWebRequest.Create(Settings.Default.syoboiProg) as HttpWebRequest;

				webRequest.UserAgent		= UserAgent;
				// 前回の取得時刻
				if( prevUpdateListGetTime.HasValue )
					webRequest.IfModifiedSince	= prevUpdateListGetTime.Value;

				WebResponse	webResponse	= webRequest.GetResponse();
				XmlReader	xmlReader	= new XmlTextReader(webResponse.GetResponseStream());

				//------------------------
				// RSSエントリをParse
				//------------------------
				while (xmlReader.Read())
				{
					if (xmlReader.NodeType == XmlNodeType.Element &&
						xmlReader.LocalName.Equals("item"))
					{
						// <item>
						SyoboiUpdate u = new SyoboiUpdate();

						while (xmlReader.Read())
						{
							if (xmlReader.NodeType == XmlNodeType.Element &&
								xmlReader.LocalName.Equals("link"))
							{
								string t;
								// <link>
								t = xmlReader.ReadElementContentAsString();
								t = t.Substring(t.IndexOf("tid/") + 4);

								u.tid = System.Convert.ToInt32(t);
							}
							else if (xmlReader.NodeType == XmlNodeType.Element &&
									   xmlReader.LocalName.Equals("pubDate"))
							{
								string t;
								t = xmlReader.ReadElementContentAsString();

								u.updateDate = ParsePubDate(t);
							}
							else if (xmlReader.NodeType == XmlNodeType.EndElement &&
									  xmlReader.LocalName.Equals("item"))
								break;
						}

						updateList.Add(u);
					}
				}
				// 次回取得時のため、今回取得時刻を記憶
				prevUpdateListGetTime = DateTime.Now;
			}
			catch(WebException ex)
			{
				bool	ignoreException = false;

				if (ex.Status == WebExceptionStatus.ProtocolError)
				{
					HttpWebResponse	httpRes = ex.Response as HttpWebResponse;

					// 前回から更新なし(304エラー)なら例外を無視
					bool	notModfied;
					notModfied	=	(httpRes != null)
								&&	(httpRes.StatusCode == HttpStatusCode.NotModified);

//#if DEBUG
					if( notModfied )
					{
						if( prevUpdateListGetTime.HasValue )
							Logger.Output(	"(しょぼかる)前回取得時("
										+	prevUpdateListGetTime.Value.ToShortTimeString()
										+	")から更新なし"	);
					}
//#endif

					ignoreException |= notModfied;
				}

				if (!ignoreException)
					throw;
			}

			return updateList;
		}

		//=========================================================================
		///	<summary>
		///		更新日付時刻を取得
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		DateTime ParsePubDate(string t)
		{
			int			y, m, d, hour, minute;
			string		[]subStrings;

			subStrings = t.Split(' ');

			//--------------
			// 月を取得
			//--------------

			string[] Months = {	"JAN","FEB","MAR","APR","MAY","JUN",
								"JUL","AUG","SEP","OCT","NOV","DEC"};

			m = 0;
			foreach (string Month in Months)
			{
				if (Month.Equals( subStrings[2].ToUpper() ))
				{
					m = Array.IndexOf(Months, Month) + 1;
					break;
				}
			}

			//--------------
			// 年日を取得
			//--------------
			y = System.Convert.ToInt32( subStrings[3] );
			d = System.Convert.ToInt32( subStrings[1] );

			//--------------
			// 時分を取得
			//--------------

			t = subStrings[4];
			hour	= Convert.ToInt32(t.Substring(0, t.IndexOf(':')));

			t = t.Substring(t.IndexOf(':') + 1);
			minute	= Convert.ToInt32(t.Substring(0, t.IndexOf(':')));

			return new DateTime(y, m, d, hour, minute, 0);
		}

		//=========================================================================
		///	<summary>
		///		番組リストをダウンロード
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		/// <history>2009/10/08 しょぼかる書式変更に合わせた改修</history>
		//=========================================================================
		public List<SyoboiProgram> DownloadPrograms()
		{
			WebClient		wc				= new WebClient();

			wc.Headers.Add( "User-Agent", UserAgent );

			Stream			s				= wc.OpenRead( Properties.Settings.Default.syoboiTitleList );
			StreamReader	streamRender	= new StreamReader(s);
			List<SyoboiProgram>	animeDataBase = new List<SyoboiProgram>();
			string			allText			= "";

			while( !streamRender.EndOfStream )
			{
				allText += streamRender.ReadLine();
			}
			allText = allText.Replace('\t', ' ');

			// 番組データを取り出す
			// <tr><td><a href="/tid/1234"> ～ </td></tr>
			Regex reg = new Regex("\\<tr\\>" +
								  "(\\s|\\n)*?(?<CONTAIN>\\<td\\>(\\s|\\n)*?<a\\s+href=\\\"/tid/[0-9]+\\\">" +
								  "(.*?)" +
								  "\\</td\\>)" +
								  "(\\s|\\n)*\\</tr\\>" );
			Match		matched = reg.Match( allText );

			for (; matched.Success; )
			{
				string strRow = matched.Groups["CONTAIN"].Value;
				ArrayList Cols = ParseTableRow( strRow );

				animeDataBase.Add(ConvertToSyoboiProgram(Cols));

				matched = matched.NextMatch();
			}

			return animeDataBase;
		}
		
		//=========================================================================
		///	<summary>
		///		指定されたTIDの番組の放送リストをダウンロードする
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		/// <history>2009/04/13 しょぼかる新仕様に合わせて改修</history>
		/// <history>2010/06/01 しょぼかる新仕様に合わせて改修</history>
		//=========================================================================
		public List<SyoboiRecord> DownloadOnAirList(
			int					tid		,	// [i] しょぼかるTID
			out string			title	)	// [o] 番組タイトル
		{
			List<String>	source;
			return DownloadOnAirList( tid, out title, out source );
		}

		public List<SyoboiRecord> DownloadOnAirList(
			int					tid		,	// [i] しょぼかるTID
			out string			title	,	// [o] 番組タイトル
			out List<String>	source	)	// [o] データソース(html)
		{
			WebClient			wc			= new WebClient();

			wc.Headers.Add("User-Agent", UserAgent);

			List<SyoboiRecord>	recordList	= new List<SyoboiRecord>();
			
			//-------------------------------------
			// URL: (しょぼかるTID)/timeを開く
			//-------------------------------------
			Stream				s			= wc.OpenRead(
												Properties.Settings.Default.syoboiTid	+
												Convert.ToString(tid)					+
												"/time"									);
			
			StreamReader		streamRender = new StreamReader(s);

			title = null;

			string strRow = "";

			// 一括取得しておく
			List<string>	lines	= new List<string>();
			string			allLine	= "";

			for (; !streamRender.EndOfStream; )
			{
				lines.Add( streamRender.ReadLine() );
			}

			foreach( string line in lines )
			{
                //---------------------------------
                // タイトルを<keywords>タグから取得
                //---------------------------------
				if (title == null &&
					line.IndexOf("\"keywords\"") >= 0)
				{
					// <meta name="keywords" content="タイトル,・・・">
					Regex	parseTitle	= new Regex(
						"(<meta)(\\s*?)(name=\\\"keywords\\\")(\\s*?)(content=\\\")(?<Title>.*?)(\\s*?)(,)(.*\\\">){1}" );
					Match	matchTitle	= parseTitle.Match(line);

					if( matchTitle.Success )
						// mod yossiepon 20150705 begin
						// title = matchTitle.Groups["Title"].Value;
						title = HttpUtility.HtmlDecode( matchTitle.Groups["Title"].Value );
						// mod yossiepon 20150705 end
				}

				allLine += line;
			}

			//--------------------------------------
			// 放送データのあるテーブルを切り出す
			//--------------------------------------	
			const string	progComment	= @"(<!)(.*?)(プログラム一覧){1}(.*?)(->)";
			Regex			regex		= new Regex( progComment + "(?<Content>.*)" + progComment );
			Match			match		= regex.Match( allLine );
			Group			group		= match.Groups["Content"];

			if (group.Success)
			{
				string tableData = group.Value;

				//------------------------
				// 放送時間テーブルの解析
				//------------------------

                // <tr...id="PIDxxxxx">...</tr>で囲まれたデータ(テーブルの1行)を切り出す
				// (<tr class="past" id="PIDxxxxx">を含む)
				string	tableFirst		= "(<tr)(\\s*?)(class=\\\"past\\\"(\\s*?))?(id=\\\"PID[0-9]{1,}\\\"){1}(.*?)(>)";
                Regex	findTableCH		= new Regex("(?<FirstPos>" + tableFirst + "(.*?)</tr>" + ")");
				Match	matchTableCH	= findTableCH.Match(tableData);

                for(; matchTableCH.Success ;)
                {
                    strRow = matchTableCH.Groups["FirstPos"].Value;

                    ParseProgramTable( strRow, recordList );

					matchTableCH = matchTableCH.NextMatch();
				}

			}

			streamRender.Close();

			source = lines;

            // add yossiepon 20160808 begin
            // 特番回に負の回数を符番する
            NumberSpecialEpisodes(ref recordList);
            // add yossiepon 20160808 end

			return recordList;
		}


		//=========================================================================
		///	<summary>
		///		放送テーブルの行データをパースして放送データを取り出す
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2009/04/13 しょぼかる新仕様に合わせて改修</history>
		//=========================================================================
		private void ParseProgramTable(
			string				strRow		,	// [i] 放送テーブルの<td></td>内の文字列
			List<SyoboiRecord>	recordList	)	// [o] 放送データ
		{
			SyoboiRecord syoboiRecord = null;
		
			try
			{
				ArrayList Cols;
				string temp;

				// mod yossiepon 20150705 begin

				List<decimal>	dummyNums		= new List<decimal>();

				//---------------------------------------------------------------------------------
				// チャンネル	開始日時	分	回数	サブタイトル/コメント	フラグ	更新日	-
				//---------------------------------------------------------------------------------
				Cols = ParseTableRow(strRow);
				
				syoboiRecord = new SyoboiRecord();

				syoboiRecord.tvStation		= (string)Cols[0];						// TV局名
				syoboiRecord.onAirDateTime	= ConvertToDateTime((string)Cols[1]);	// 開始日時
				syoboiRecord.length			= int.Parse( (string)Cols[ 2 ] );		// 尺

				// 回数が無い場合
				if ( Cols[ 3 ].Equals( "" ) )
				{
					//-----------------------------
					// 複数話連続放送の対策
					//-----------------------------

					// サブタイトルから#nn～#mmを抽出
					Regex parser	= new Regex( "^#(?<FirstEpisode>[0-9.]+)～#(?<LastEpisode>[0-9.]+)" );
					Match match		= parser.Match( (string)Cols[ 4 ] );

					// マッチした場合
					if (match.Success)
					{
						decimal firstEpisode = Decimal.Parse(match.Groups["FirstEpisode"].Value);
						decimal lastEpisode	= Decimal.Parse(match.Groups["LastEpisode"].Value);

						// 連続話数は分割せずにまとめて録画する

                        // 話番号文字列（マッチ結果全体をそのまま入れる）
                        syoboiRecord.episode = formatEpisodeNo(firstEpisode) + "～" + formatEpisodeNo(lastEpisode);	
						syoboiRecord.number	= convertDecimalEpisodeNoToInt(firstEpisode);	// 話番号（１つ目）

						// HTMLエンコード文字をデコード
						syoboiRecord.subtitle =	HttpUtility.HtmlDecode(MakeNaked((string)Cols[4]));	// サブタイトル

						// 最初の話数が端数だった場合
						if ( convertDecimalEpisodeNoToInt(firstEpisode) == UNNUMBERED_EPISODE )
						{
							// 端数のままダミー話数リストに追加する
							dummyNums.Add(firstEpisode);
						}

						// 最初の話数から最後の話数までをダミー話数リストに追加する
                        // 最初の話数が端数の場合はその次から、最後の話数が端数の場合はその前までが追加される
                        for (int i = Decimal.ToInt32(Decimal.Ceiling(firstEpisode)); i <= Decimal.ToInt32(Decimal.Floor(lastEpisode)); i++)
						{
							dummyNums.Add(i);
						}

						// 最後の話数が端数だった場合
						if ( convertDecimalEpisodeNoToInt(lastEpisode) == UNNUMBERED_EPISODE )
						{
							// 端数のままダミー話数リストに追加する
							dummyNums.Add(lastEpisode);
						}
					}
					else
					{
						//------------------------------------------------
						// 複数話連続「#1(「～」)(、#2(「～」))...」
						// ※1話のみで回数が無い場合もここで処理される(#10.5等)
						//------------------------------------------------
						//List<string>	episodeStrs		= new List<string>();
						List<string>	subTitles		= new List<string>();

						// サブタイトルから#nn「サブタイトル」、#mm「サブタイトル」...を抽出
						// ※「」をつけずにサブタイトルが入る場合があるので、その場合は繰り返しを考慮せずにすべて抜き出す
						parser = new Regex("#(?<EpisodeNumber>[0-9.]+)( (?<Subtitle>.+)|(｢|「)(?<Subtitle>.*?)(｣|」))?(、)?");
						match	= parser.Match((string)Cols[4]);

						// エピソード番号とタイトルを全て切り出す
						while(match.Success)
						{
							string	episodeStr	= match.Groups["EpisodeNumber"].Value;
							decimal	episodeNum	= Decimal.Parse(episodeStr);
							string	subTitle	= HttpUtility.HtmlDecode( (string)match.Groups["Subtitle"].Value );

							//episodeStrs.Add( episodeStr	);
							dummyNums.Add( episodeNum );
							subTitles.Add( subTitle );

							match =	match.NextMatch();
						}

						// 抽出できなかった場合
						if (dummyNums.Count == 0)
						{
							syoboiRecord.episode =	"";					// 話番号文字列
							syoboiRecord.number	=	UNNUMBERED_EPISODE;	// 話番号 = 特別編（UNNUMBERED_EPISODE）

							// HTMLエンコード文字をデコード
							syoboiRecord.subtitle =	HttpUtility.HtmlDecode(MakeNaked((string)Cols[4]));	// サブタイトル
						}
						else
						{
							int firstEpisode	=	convertDecimalEpisodeNoToInt( dummyNums[0] );
							string episodeStr	= "";

							// 話番号が複数存在する場合
							if (dummyNums.Count > 1)
							{
								int lastEpisode =	convertDecimalEpisodeNoToInt( dummyNums[dummyNums.Count -1] );
								Boolean isEntireNums = true;

								// 区切り内の番号が連番になっているかチェックする
								for(int i = 1; i < dummyNums.Count; i++) {

									if( dummyNums[i] != firstEpisode + i ) {

										isEntireNums = false;
										break;									
									}								
								}

								// 連番の場合、「nn～mm」にする
								if ( isEntireNums )
								{
									episodeStr = formatEpisodeNo(firstEpisode) + "～" + formatEpisodeNo(lastEpisode);
								}
							}

							// 連番でない場合、「nn(、mm、...)」にする
							if( episodeStr.Length == 0 )
							{
								StringBuilder episodeStrBuf = new StringBuilder();

								for(int i = 0; i < dummyNums.Count; i++) {

									if( episodeStrBuf.Length > 0 )
									{
										episodeStrBuf.Append('、');
									}
									episodeStrBuf.Append(formatEpisodeNo(dummyNums[i]));
								}

								episodeStr = episodeStrBuf.ToString();
							}

							// サブタイトルを「～(｜～｜…)」にする
							StringBuilder subtitleBuf = new StringBuilder();

							for(int i = 0; i < subTitles.Count; i++) {

								if( subtitleBuf.Length > 0 )
								{
									subtitleBuf.Append('｜');
								}
								subtitleBuf.Append(subTitles[i]);
							}

							syoboiRecord.episode =	episodeStr;					// 話番号文字列
							syoboiRecord.number	=	firstEpisode;				// 話番号

							syoboiRecord.subtitle = subtitleBuf.ToString();		// サブタイトル
						}
					}
				}
				else
				{
                    decimal storyNo         = decimal.Parse((string)Cols[3]);
                    syoboiRecord.number		= convertDecimalEpisodeNoToInt(storyNo);    // 話番号
                    if (storyNo != decimal.Zero)
                    {
                        syoboiRecord.episode = formatEpisodeNo(syoboiRecord.number);    // 話番号文字列
                    }
                    else
                    {
                        // 0話で話番号が振られているケースの場合
                        syoboiRecord.episode = formatEpisodeNo(storyNo);                // 話番号文字列
                    }

					// HTMLエンコード文字をデコード
					syoboiRecord.subtitle = HttpUtility.HtmlDecode( MakeNaked( (string)Cols[ 4 ] ) ); // サブタイトル

				}

				recordList.Add( syoboiRecord );

				//-----------------------------
				// 連続話数の残り分、データを追加
				//-----------------------------
				for	(int i = 1; i < dummyNums.Count; ++i)
				{
					SyoboiRecord newRecord;

					newRecord =	(SyoboiRecord)syoboiRecord.Clone();

					newRecord.length = 0;											// 尺を0にして無効扱いにする
					newRecord.number = convertDecimalEpisodeNoToInt(dummyNums[i]);	// 話番号
					//newRecord.subtitle = "";										// サブタイトルをクリアする

					recordList.Add(newRecord);
				}

				// mod yossiepon 20150705 end
			}
			catch(Exception)
			{
#if _DEBUG
//				if (syoboiRecord!=null)
//					Console.WriteLine(title + " " + syoboiRecord.number);
#endif
			}
		}

		//=========================================================================
		///	<summary>
		///		放送リストから放送局をリストアップする
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		public ArrayList ListupTvStation(List<SyoboiCalender.SyoboiRecord> recordList)
		{
			ArrayList tvStationList = new ArrayList();
		
			foreach(SyoboiCalender.SyoboiRecord record in recordList)
			{
				bool addToList = true;

				foreach(string tvStation in tvStationList)
				{
					if (record.tvStation.Equals(tvStation))
					{
						addToList = false;
						break;
					}
				}
				
				if (addToList)
					tvStationList.Add(record.tvStation);
			}
		
			return 	tvStationList;
		}

		//=========================================================================
		///	<summary>
		///		番組のhtmlテーブルから内部の構造体形式に変換
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private SyoboiProgram ConvertToSyoboiProgram(ArrayList Cols)
		{
			SyoboiProgram newProgram = new SyoboiProgram();

			newProgram.title = (string)Cols[0];
			newProgram.tid = int.Parse((string)Cols[3]);

			newProgram.seasonOnAir.RecordState = 0;
			
			string seasonOnAir = (string)Cols[1];
			
			seasonOnAir = seasonOnAir.Trim();
			if( seasonOnAir.Equals("-------") )
			{
			}else{
				string year, month;

				year = seasonOnAir.Substring( 0, 4 );
				if( !year.Equals("----") )
				{
					newProgram.seasonOnAir.RecordState |= SyoboiProgram.SeasonOnAir.YearDecided;
					newProgram.seasonOnAir.year = int.Parse(year);
				}
				
				month	= seasonOnAir.Substring( seasonOnAir.Length - 2 );
				if(! month.Equals("--") )
				{
					newProgram.seasonOnAir.RecordState |= SyoboiProgram.SeasonOnAir.MonthDecided;
					newProgram.seasonOnAir.month	= int.Parse( month );
				}

			}
		
			return newProgram;
		}
		
		//=========================================================================
		///	<summary>
		///		しょぼかるの日付書式からDateTimeに変換
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private DateTime ConvertToDateTime(
			string strDateTime)	// [i] 日時文字列「2009-02-02 (月) 10:30」
		{
			DateTime dateTime;
			int		year;
			int		month;
			int		day;
			int		hour;
			int		minute;
			bool incDay = false;

			Regex	regex = new Regex(
				"(?<Year>[0-9]{4})-(?<Month>[0-9]{1,})-(?<Day>[0-9]{1,})( *?)(.*?)( *?)" +
				"(?<Hour>[0-9]{1,}):(?<Minute>[0-9]{1,})");
			Match	match = regex.Match( strDateTime );

			year	= int.Parse( match.Groups["Year"].Value );
			month	= int.Parse(match.Groups["Month"].Value);
			day		= int.Parse(match.Groups["Day"].Value);
			hour	= int.Parse(match.Groups["Hour"].Value);
			minute	= int.Parse(match.Groups["Minute"].Value);


			// 24:00以降なら+1日
			if (hour >= 24)
			{
				hour -= 24;
				incDay = true;
			}

			dateTime = new DateTime(
				year	,
				month	,
				day		,
				hour	,
				minute	,
				0		);

			if (incDay)
				dateTime = dateTime.AddDays(1.0);

			return dateTime;
		}

		//=========================================================================
		///	<summary>
		///		htmlタグを排除する
		///	</summary>
		/// <remarks>
		///		とりあえず、頻繁に出てくる<a><div>を削除する。
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private string MakeNaked(string context)
		{
			string nakedContext = context;

			// add yossiepon 20150705 begin
			{
				Regex regex = new Regex("<div class=\"peComment\">(?<Content>(.*?))</div>");
				Match match = regex.Match(nakedContext);

				if (match.Groups["Content"].Success)
				{
					string content = " " + match.Groups["Content"].Value;

					{
						Regex regex2 = new Regex("<a(.*?)>(?<Content>(.*?))</a>");
						Match match2 = regex2.Match(content);

						if (match2.Groups["Content"].Success)
						{
							content = regex2.Replace(content, "");
						}
					}

					nakedContext = regex.Replace(nakedContext, content);
				}
			}
			// add yossiepon 20150705 end

			{
				Regex regex = new Regex("<a(.*?)>(?<Content>(.*?))</a>");
				Match match = regex.Match(nakedContext);

				if (match.Groups["Content"].Success)
				{
					string content = match.Groups["Content"].Value;

					nakedContext = regex.Replace(context, content);
				}
			}

			// <TAG*>xxxx</TAG>形式のタグを除去
			string[]	targetTags = {"div", "span"};

			foreach( string target in targetTags )
			{
				Regex regex = new Regex("<" + target +"(.*?)>(.*?)</" + target + ">");
				Match match = regex.Match(nakedContext);

				if (match.Success)
				{
					nakedContext = regex.Replace(nakedContext, "");
				}
			}

            // mod yossiepon 20160924 begin
            // // mod yossiepon 20150705 begin
            // // return nakedContext;
            // return nakedContext.Trim();
            // // mod yossiepon 20150705 end

            nakedContext = nakedContext.Trim();
            // 特番回に含まれる先頭の「^」があれば除去する
            if ((nakedContext.Length > 0) && (nakedContext[0] == '^'))
            {
                nakedContext = nakedContext.Substring(1);
            }

            return nakedContext;
            // mod yossiepon 20160924 end
		}
		
		//=========================================================================
		///	<summary>
		///		HTMLのテーブルの行を<TD>...</TD>列ごとに分解する
		///	</summary>
		/// <remarks>
		///		<tr></tr>セクションで囲まれた内側の文字列を渡す
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private ArrayList ParseTableRow(string strRow)
		{
			ArrayList Cols = new ArrayList();
			string strCol;

			Regex	regex	= new Regex("(<td)(.*?)(>)(?<Content>(.*?))(</td>)");
			Match	match	= regex.Match( strRow );

			for (; match.Success; )
			{
				string	content = (string)match.Groups["Content"].Value;
				Cols.Add( MakeNaked( content ) );
				match = match.NextMatch();
			}
			
			return Cols;
		}
		
		//=========================================================================
		///	<summary>
		///		指定されたタグで囲まれた範囲の文字列を返す
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private string ExtractTagContain(
			string		line	,
			string		tagName	)
		{
			string dummy;
			
			return ExtractTagContain( line, tagName , out dummy );
		}

		//=========================================================================
		///	<summary>
		///		指定されたタグで囲まれた範囲の文字列と残りの文字列を返す
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX 新規作成</history>
		//=========================================================================
		private string ExtractTagContain(
			string		line		,	// [i] 文字列
			string		tagName		,	// [i] タグ名
			out string	remain		)	// [o] 残り文字列
		{
			try
			{
				Regex reg = new Regex(@"\<" + tagName + @"\>(?<CONTAIN>.*?)\</" + tagName + @"\>");
				Match matched = reg.Match(line);

				if (matched.Success)
				{
					string contain = matched.Groups["CONTAIN"].Value;
					remain = reg.Replace(line, "");
					return contain;
				}
			}
			catch (Exception ex)
			{
			}

			remain = line;
			return "";
		}


        // add yossiepon 20160808 begin
		//=========================================================================
		///	<summary>
        ///		特番回に負の話数を符番する
		///	</summary>
		///	<remarks>
		///	</remarks>
		///	<history>2006/XX/XX	新規作成</history>
		//=========================================================================
        private void NumberSpecialEpisodes(ref List<SyoboiCalender.SyoboiRecord> syoboi)
		{
            ArrayList tvStationList = ListupTvStation(syoboi);

            foreach (string tvStation in tvStationList) {

                int curSpEpNo = -1;

			Dictionary<string, int> specialEpNos = new Dictionary<string, int>();

                // 特番回に話数を振る
                for (int i = 0; i < syoboi.Count; i++)
			{
                    if (syoboi[i].tvStation.Equals(tvStation))
				{
                        int epNo = syoboi[i].number;
                        string subtitle = syoboi[i].subtitle;

                        // 話数が特番回（UNNUMBERED_EPISODE）なら
					if (epNo == UNNUMBERED_EPISODE)
					{
						// 特別編の話数がディクショナリに存在したら	
                            if (specialEpNos.ContainsKey(subtitle))
						{
							// 保存済みの話数を使用する
							epNo = specialEpNos[subtitle];
						}
						else
						{
							// 存在しなければ、新しい話数を発番して保存する
                                epNo = curSpEpNo--;
							specialEpNos.Add(subtitle, epNo);
						}

						// 話数文字列が振られていなければ「SPn」にする
                            if (syoboi[i].episode.Length == 0)
						{
                                syoboi[i].episode = Settings.Default.unnumberedEpisodePrefix + (-epNo);
						}

                            // 特番回に話数をつける
                            syoboi[i].number = epNo;
                        }
					}
				}
			}
		}
        // add yossiepon 20160808 end
		
        // add yossiepon 20150705 begin
		//=========================================================================
		/// <summary>
		///		話番号を整数に変換する
		/// </summary>
		/// <param name="no">話番号（実数）</param>
		/// <returns>1以上の整数の場合そのまま、0または実数の場合は UNNUMBERED_EPISODE にする</returns>
		//=========================================================================
		private int convertDecimalEpisodeNoToInt(decimal no)
		{
			// 端数回は特別編（UNNUMBERED_EPISODE）として処理
			if( no.CompareTo(Decimal.Round(no)) != 0 )
			{
				return UNNUMBERED_EPISODE;
			}
			// 0話は特別編（UNNUMBERED_EPISODE）として処理
			else if( no.Equals(Decimal.Zero) )
			{
				return UNNUMBERED_EPISODE;
			}

			// 端数でなければ整数なので、そのまま変換して返す
			return Decimal.ToInt32(no);
		}
        // add yossiepon 20150705 end

        // add yossiepon 20160924 begin
        //=========================================================================
        /// <summary>
        ///		話数に書式を適用する
        /// </summary>
        /// <param name="no">話番号（実数）</param>
        /// <returns>書式を適用した話数文字列</returns>
        //=========================================================================
        private String formatEpisodeNo(decimal no)
        {
            return no.ToString(Settings.Default.storyNoFormat);
        }
        // add yossiepon 20160924 end
		
	}

}
