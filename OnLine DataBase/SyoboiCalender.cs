//=========================================================================
///	<summary>
///		����ڂ��J�����_�[ �I�����C���f�[�^�擾�N���X
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2006/XX/XX �V�K�쐬 Dr.Kurusugawa</history>
/// <history>2010/05/01 Subversion�ŊǗ����邽�ߕs�v�ȃR�����g�폜</history>
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
	///		����ڂ��J�����_�[�f�[�^�擾�N���X
	///	</summary>
	/// <remarks>
	/// </remarks>
	/// <history>2006/XX/XX �V�K�쐬</history>
	//=========================================================================
	class SyoboiCalender
	{
		// add yossiepon 20150705 begin
		public const int UNNUMBERED_EPISODE = int.MinValue;
        // add yossiepon 20150705 end

		DateTime?	prevUpdateListGetTime	= null;			// �O��̍X�V���X�g�擾����

		// �^�C�g�����X�g�e�[�u��(http://cal.syoboi.jp/titlelist.php)
		// �^�C�g��	�������	����I��	Point	TID	�X�V
		
		//=========================================================================
		///	<summary>
		///		����ڂ���̔ԑg�f�[�^��ێ�����N���X
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
		///		����ڂ���̃G�s�\�[�h�f�[�^��ێ�����N���X
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
			///		�t�H�[�}�b�g���ꂽ�ԑg�G�s�\�[�h�������Ԃ�
			///	</summary>
			/// <remarks>
			/// </remarks>
			/// <history>2006/XX/XX �V�K�쐬</history>
			//=========================================================================
			public override string ToString()
			{
				return tvStation + "-No." + number + "(" + episode + ")�u" + subtitle + "�v(" + length + " min.)/" + onAirDateTime;
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
		///		mAgicAnime�ŗL��User-Agent�������Ԃ�
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2007/12/22 �V�K�쐬</history>
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
		///		����ڂ���RSS����X�V���ꂽ�ԑg���X�g���擾
		///	</summary>
		/// <remarks>
		///		�X�V���Ȃ��ꍇ�͋�̃��X�g��Ԃ��B
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		/// <history>2009/02/11 ����ڂ��镉�ב΍�̂���if-modified-since�ǉ�</history>
		//=========================================================================
		public List<SyoboiUpdate> DownloadUpdateList()
		{
			List<SyoboiUpdate> updateList = new List<SyoboiUpdate>();

			try
			{
				//------------------------
				// ����ڂ���RSS���J��
				//------------------------
				HttpWebRequest webRequest =
					HttpWebRequest.Create(Settings.Default.syoboiProg) as HttpWebRequest;

				webRequest.UserAgent		= UserAgent;
				// �O��̎擾����
				if( prevUpdateListGetTime.HasValue )
					webRequest.IfModifiedSince	= prevUpdateListGetTime.Value;

				WebResponse	webResponse	= webRequest.GetResponse();
				XmlReader	xmlReader	= new XmlTextReader(webResponse.GetResponseStream());

				//------------------------
				// RSS�G���g����Parse
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
				// ����擾���̂��߁A����擾�������L��
				prevUpdateListGetTime = DateTime.Now;
			}
			catch(WebException ex)
			{
				bool	ignoreException = false;

				if (ex.Status == WebExceptionStatus.ProtocolError)
				{
					HttpWebResponse	httpRes = ex.Response as HttpWebResponse;

					// �O�񂩂�X�V�Ȃ�(304�G���[)�Ȃ��O�𖳎�
					bool	notModfied;
					notModfied	=	(httpRes != null)
								&&	(httpRes.StatusCode == HttpStatusCode.NotModified);

//#if DEBUG
					if( notModfied )
					{
						if( prevUpdateListGetTime.HasValue )
							Logger.Output(	"(����ڂ���)�O��擾��("
										+	prevUpdateListGetTime.Value.ToShortTimeString()
										+	")����X�V�Ȃ�"	);
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
		///		�X�V���t�������擾
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		//=========================================================================
		DateTime ParsePubDate(string t)
		{
			int			y, m, d, hour, minute;
			string		[]subStrings;

			subStrings = t.Split(' ');

			//--------------
			// �����擾
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
			// �N�����擾
			//--------------
			y = System.Convert.ToInt32( subStrings[3] );
			d = System.Convert.ToInt32( subStrings[1] );

			//--------------
			// �������擾
			//--------------

			t = subStrings[4];
			hour	= Convert.ToInt32(t.Substring(0, t.IndexOf(':')));

			t = t.Substring(t.IndexOf(':') + 1);
			minute	= Convert.ToInt32(t.Substring(0, t.IndexOf(':')));

			return new DateTime(y, m, d, hour, minute, 0);
		}

		//=========================================================================
		///	<summary>
		///		�ԑg���X�g���_�E�����[�h
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		/// <history>2009/10/08 ����ڂ��鏑���ύX�ɍ��킹�����C</history>
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

			// �ԑg�f�[�^�����o��
			// <tr><td><a href="/tid/1234"> �` </td></tr>
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
		///		�w�肳�ꂽTID�̔ԑg�̕������X�g���_�E�����[�h����
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		/// <history>2009/04/13 ����ڂ���V�d�l�ɍ��킹�ĉ��C</history>
		/// <history>2010/06/01 ����ڂ���V�d�l�ɍ��킹�ĉ��C</history>
		//=========================================================================
		public List<SyoboiRecord> DownloadOnAirList(
			int					tid		,	// [i] ����ڂ���TID
			out string			title	)	// [o] �ԑg�^�C�g��
		{
			List<String>	source;
			return DownloadOnAirList( tid, out title, out source );
		}

		public List<SyoboiRecord> DownloadOnAirList(
			int					tid		,	// [i] ����ڂ���TID
			out string			title	,	// [o] �ԑg�^�C�g��
			out List<String>	source	)	// [o] �f�[�^�\�[�X(html)
		{
			WebClient			wc			= new WebClient();

			wc.Headers.Add("User-Agent", UserAgent);

			List<SyoboiRecord>	recordList	= new List<SyoboiRecord>();
			
			//-------------------------------------
			// URL: (����ڂ���TID)/time���J��
			//-------------------------------------
			Stream				s			= wc.OpenRead(
												Properties.Settings.Default.syoboiTid	+
												Convert.ToString(tid)					+
												"/time"									);
			
			StreamReader		streamRender = new StreamReader(s);

			title = null;

			string strRow = "";

			// �ꊇ�擾���Ă���
			List<string>	lines	= new List<string>();
			string			allLine	= "";

			for (; !streamRender.EndOfStream; )
			{
				lines.Add( streamRender.ReadLine() );
			}

			foreach( string line in lines )
			{
                //---------------------------------
                // �^�C�g����<keywords>�^�O����擾
                //---------------------------------
				if (title == null &&
					line.IndexOf("\"keywords\"") >= 0)
				{
					// <meta name="keywords" content="�^�C�g��,�E�E�E">
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
			// �����f�[�^�̂���e�[�u����؂�o��
			//--------------------------------------	
			const string	progComment	= @"(<!)(.*?)(�v���O�����ꗗ){1}(.*?)(->)";
			Regex			regex		= new Regex( progComment + "(?<Content>.*)" + progComment );
			Match			match		= regex.Match( allLine );
			Group			group		= match.Groups["Content"];

			if (group.Success)
			{
				string tableData = group.Value;

				//------------------------
				// �������ԃe�[�u���̉��
				//------------------------

                // <tr...id="PIDxxxxx">...</tr>�ň͂܂ꂽ�f�[�^(�e�[�u����1�s)��؂�o��
				// (<tr class="past" id="PIDxxxxx">���܂�)
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
            // ���ԉ�ɕ��̉񐔂𕄔Ԃ���
            NumberSpecialEpisodes(ref recordList);
            // add yossiepon 20160808 end

			return recordList;
		}


		//=========================================================================
		///	<summary>
		///		�����e�[�u���̍s�f�[�^���p�[�X���ĕ����f�[�^�����o��
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2009/04/13 ����ڂ���V�d�l�ɍ��킹�ĉ��C</history>
		//=========================================================================
		private void ParseProgramTable(
			string				strRow		,	// [i] �����e�[�u����<td></td>���̕�����
			List<SyoboiRecord>	recordList	)	// [o] �����f�[�^
		{
			SyoboiRecord syoboiRecord = null;
		
			try
			{
				ArrayList Cols;
				string temp;

				// mod yossiepon 20150705 begin

				List<decimal>	dummyNums		= new List<decimal>();

				//---------------------------------------------------------------------------------
				// �`�����l��	�J�n����	��	��	�T�u�^�C�g��/�R�����g	�t���O	�X�V��	-
				//---------------------------------------------------------------------------------
				Cols = ParseTableRow(strRow);
				
				syoboiRecord = new SyoboiRecord();

				syoboiRecord.tvStation		= (string)Cols[0];						// TV�ǖ�
				syoboiRecord.onAirDateTime	= ConvertToDateTime((string)Cols[1]);	// �J�n����
				syoboiRecord.length			= int.Parse( (string)Cols[ 2 ] );		// ��

				// �񐔂������ꍇ
				if ( Cols[ 3 ].Equals( "" ) )
				{
					//-----------------------------
					// �����b�A�������̑΍�
					//-----------------------------

					// �T�u�^�C�g������#nn�`#mm�𒊏o
					Regex parser	= new Regex( "^#(?<FirstEpisode>[0-9.]+)�`#(?<LastEpisode>[0-9.]+)" );
					Match match		= parser.Match( (string)Cols[ 4 ] );

					// �}�b�`�����ꍇ
					if (match.Success)
					{
						decimal firstEpisode = Decimal.Parse(match.Groups["FirstEpisode"].Value);
						decimal lastEpisode	= Decimal.Parse(match.Groups["LastEpisode"].Value);

						// �A���b���͕��������ɂ܂Ƃ߂Ę^�悷��

                        // �b�ԍ�������i�}�b�`���ʑS�̂����̂܂ܓ����j
                        syoboiRecord.episode = formatEpisodeNo(firstEpisode) + "�`" + formatEpisodeNo(lastEpisode);	
						syoboiRecord.number	= convertDecimalEpisodeNoToInt(firstEpisode);	// �b�ԍ��i�P�ځj

						// HTML�G���R�[�h�������f�R�[�h
						syoboiRecord.subtitle =	HttpUtility.HtmlDecode(MakeNaked((string)Cols[4]));	// �T�u�^�C�g��

						// �ŏ��̘b�����[���������ꍇ
						if ( convertDecimalEpisodeNoToInt(firstEpisode) == UNNUMBERED_EPISODE )
						{
							// �[���̂܂܃_�~�[�b�����X�g�ɒǉ�����
							dummyNums.Add(firstEpisode);
						}

						// �ŏ��̘b������Ō�̘b���܂ł��_�~�[�b�����X�g�ɒǉ�����
                        // �ŏ��̘b�����[���̏ꍇ�͂��̎�����A�Ō�̘b�����[���̏ꍇ�͂��̑O�܂ł��ǉ������
                        for (int i = Decimal.ToInt32(Decimal.Ceiling(firstEpisode)); i <= Decimal.ToInt32(Decimal.Floor(lastEpisode)); i++)
						{
							dummyNums.Add(i);
						}

						// �Ō�̘b�����[���������ꍇ
						if ( convertDecimalEpisodeNoToInt(lastEpisode) == UNNUMBERED_EPISODE )
						{
							// �[���̂܂܃_�~�[�b�����X�g�ɒǉ�����
							dummyNums.Add(lastEpisode);
						}
					}
					else
					{
						//------------------------------------------------
						// �����b�A���u#1(�u�`�v)(�A#2(�u�`�v))...�v
						// ��1�b�݂̂ŉ񐔂������ꍇ�������ŏ��������(#10.5��)
						//------------------------------------------------
						//List<string>	episodeStrs		= new List<string>();
						List<string>	subTitles		= new List<string>();

						// �T�u�^�C�g������#nn�u�T�u�^�C�g���v�A#mm�u�T�u�^�C�g���v...�𒊏o
						// ���u�v�������ɃT�u�^�C�g��������ꍇ������̂ŁA���̏ꍇ�͌J��Ԃ����l�������ɂ��ׂĔ����o��
						parser = new Regex("#(?<EpisodeNumber>[0-9.]+)( (?<Subtitle>.+)|(�|�u)(?<Subtitle>.*?)(�|�v))?(�A)?");
						match	= parser.Match((string)Cols[4]);

						// �G�s�\�[�h�ԍ��ƃ^�C�g����S�Đ؂�o��
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

						// ���o�ł��Ȃ������ꍇ
						if (dummyNums.Count == 0)
						{
							syoboiRecord.episode =	"";					// �b�ԍ�������
							syoboiRecord.number	=	UNNUMBERED_EPISODE;	// �b�ԍ� = ���ʕҁiUNNUMBERED_EPISODE�j

							// HTML�G���R�[�h�������f�R�[�h
							syoboiRecord.subtitle =	HttpUtility.HtmlDecode(MakeNaked((string)Cols[4]));	// �T�u�^�C�g��
						}
						else
						{
							int firstEpisode	=	convertDecimalEpisodeNoToInt( dummyNums[0] );
							string episodeStr	= "";

							// �b�ԍ����������݂���ꍇ
							if (dummyNums.Count > 1)
							{
								int lastEpisode =	convertDecimalEpisodeNoToInt( dummyNums[dummyNums.Count -1] );
								Boolean isEntireNums = true;

								// ��؂���̔ԍ����A�ԂɂȂ��Ă��邩�`�F�b�N����
								for(int i = 1; i < dummyNums.Count; i++) {

									if( dummyNums[i] != firstEpisode + i ) {

										isEntireNums = false;
										break;									
									}								
								}

								// �A�Ԃ̏ꍇ�A�unn�`mm�v�ɂ���
								if ( isEntireNums )
								{
									episodeStr = formatEpisodeNo(firstEpisode) + "�`" + formatEpisodeNo(lastEpisode);
								}
							}

							// �A�ԂłȂ��ꍇ�A�unn(�Amm�A...)�v�ɂ���
							if( episodeStr.Length == 0 )
							{
								StringBuilder episodeStrBuf = new StringBuilder();

								for(int i = 0; i < dummyNums.Count; i++) {

									if( episodeStrBuf.Length > 0 )
									{
										episodeStrBuf.Append('�A');
									}
									episodeStrBuf.Append(formatEpisodeNo(dummyNums[i]));
								}

								episodeStr = episodeStrBuf.ToString();
							}

							// �T�u�^�C�g�����u�`(�b�`�b�c)�v�ɂ���
							StringBuilder subtitleBuf = new StringBuilder();

							for(int i = 0; i < subTitles.Count; i++) {

								if( subtitleBuf.Length > 0 )
								{
									subtitleBuf.Append('�b');
								}
								subtitleBuf.Append(subTitles[i]);
							}

							syoboiRecord.episode =	episodeStr;					// �b�ԍ�������
							syoboiRecord.number	=	firstEpisode;				// �b�ԍ�

							syoboiRecord.subtitle = subtitleBuf.ToString();		// �T�u�^�C�g��
						}
					}
				}
				else
				{
                    decimal storyNo         = decimal.Parse((string)Cols[3]);
                    syoboiRecord.number		= convertDecimalEpisodeNoToInt(storyNo);    // �b�ԍ�
                    if (storyNo != decimal.Zero)
                    {
                        syoboiRecord.episode = formatEpisodeNo(syoboiRecord.number);    // �b�ԍ�������
                    }
                    else
                    {
                        // 0�b�Řb�ԍ����U���Ă���P�[�X�̏ꍇ
                        syoboiRecord.episode = formatEpisodeNo(storyNo);                // �b�ԍ�������
                    }

					// HTML�G���R�[�h�������f�R�[�h
					syoboiRecord.subtitle = HttpUtility.HtmlDecode( MakeNaked( (string)Cols[ 4 ] ) ); // �T�u�^�C�g��

				}

				recordList.Add( syoboiRecord );

				//-----------------------------
				// �A���b���̎c�蕪�A�f�[�^��ǉ�
				//-----------------------------
				for	(int i = 1; i < dummyNums.Count; ++i)
				{
					SyoboiRecord newRecord;

					newRecord =	(SyoboiRecord)syoboiRecord.Clone();

					newRecord.length = 0;											// �ڂ�0�ɂ��Ė��������ɂ���
					newRecord.number = convertDecimalEpisodeNoToInt(dummyNums[i]);	// �b�ԍ�
					//newRecord.subtitle = "";										// �T�u�^�C�g�����N���A����

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
		///		�������X�g��������ǂ����X�g�A�b�v����
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
		///		�ԑg��html�e�[�u����������̍\���̌`���ɕϊ�
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
		///		����ڂ���̓��t��������DateTime�ɕϊ�
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		//=========================================================================
		private DateTime ConvertToDateTime(
			string strDateTime)	// [i] ����������u2009-02-02 (��) 10:30�v
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


			// 24:00�ȍ~�Ȃ�+1��
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
		///		html�^�O��r������
		///	</summary>
		/// <remarks>
		///		�Ƃ肠�����A�p�ɂɏo�Ă���<a><div>���폜����B
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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

			// <TAG*>xxxx</TAG>�`���̃^�O������
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
            // ���ԉ�Ɋ܂܂��擪�́u^�v������Ώ�������
            if ((nakedContext.Length > 0) && (nakedContext[0] == '^'))
            {
                nakedContext = nakedContext.Substring(1);
            }

            return nakedContext;
            // mod yossiepon 20160924 end
		}
		
		//=========================================================================
		///	<summary>
		///		HTML�̃e�[�u���̍s��<TD>...</TD>�񂲂Ƃɕ�������
		///	</summary>
		/// <remarks>
		///		<tr></tr>�Z�N�V�����ň͂܂ꂽ�����̕������n��
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
		///		�w�肳�ꂽ�^�O�ň͂܂ꂽ�͈͂̕������Ԃ�
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
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
		///		�w�肳�ꂽ�^�O�ň͂܂ꂽ�͈͂̕�����Ǝc��̕������Ԃ�
		///	</summary>
		/// <remarks>
		/// </remarks>
		/// <history>2006/XX/XX �V�K�쐬</history>
		//=========================================================================
		private string ExtractTagContain(
			string		line		,	// [i] ������
			string		tagName		,	// [i] �^�O��
			out string	remain		)	// [o] �c�蕶����
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
        ///		���ԉ�ɕ��̘b���𕄔Ԃ���
		///	</summary>
		///	<remarks>
		///	</remarks>
		///	<history>2006/XX/XX	�V�K�쐬</history>
		//=========================================================================
        private void NumberSpecialEpisodes(ref List<SyoboiCalender.SyoboiRecord> syoboi)
		{
            ArrayList tvStationList = ListupTvStation(syoboi);

            foreach (string tvStation in tvStationList) {

                int curSpEpNo = -1;

			Dictionary<string, int> specialEpNos = new Dictionary<string, int>();

                // ���ԉ�ɘb����U��
                for (int i = 0; i < syoboi.Count; i++)
			{
                    if (syoboi[i].tvStation.Equals(tvStation))
				{
                        int epNo = syoboi[i].number;
                        string subtitle = syoboi[i].subtitle;

                        // �b�������ԉ�iUNNUMBERED_EPISODE�j�Ȃ�
					if (epNo == UNNUMBERED_EPISODE)
					{
						// ���ʕ҂̘b�����f�B�N�V���i���ɑ��݂�����	
                            if (specialEpNos.ContainsKey(subtitle))
						{
							// �ۑ��ς݂̘b�����g�p����
							epNo = specialEpNos[subtitle];
						}
						else
						{
							// ���݂��Ȃ���΁A�V�����b���𔭔Ԃ��ĕۑ�����
                                epNo = curSpEpNo--;
							specialEpNos.Add(subtitle, epNo);
						}

						// �b�������񂪐U���Ă��Ȃ���΁uSPn�v�ɂ���
                            if (syoboi[i].episode.Length == 0)
						{
                                syoboi[i].episode = Settings.Default.unnumberedEpisodePrefix + (-epNo);
						}

                            // ���ԉ�ɘb��������
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
		///		�b�ԍ��𐮐��ɕϊ�����
		/// </summary>
		/// <param name="no">�b�ԍ��i�����j</param>
		/// <returns>1�ȏ�̐����̏ꍇ���̂܂܁A0�܂��͎����̏ꍇ�� UNNUMBERED_EPISODE �ɂ���</returns>
		//=========================================================================
		private int convertDecimalEpisodeNoToInt(decimal no)
		{
			// �[����͓��ʕҁiUNNUMBERED_EPISODE�j�Ƃ��ď���
			if( no.CompareTo(Decimal.Round(no)) != 0 )
			{
				return UNNUMBERED_EPISODE;
			}
			// 0�b�͓��ʕҁiUNNUMBERED_EPISODE�j�Ƃ��ď���
			else if( no.Equals(Decimal.Zero) )
			{
				return UNNUMBERED_EPISODE;
			}

			// �[���łȂ���ΐ����Ȃ̂ŁA���̂܂ܕϊ����ĕԂ�
			return Decimal.ToInt32(no);
		}
        // add yossiepon 20150705 end

        // add yossiepon 20160924 begin
        //=========================================================================
        /// <summary>
        ///		�b���ɏ�����K�p����
        /// </summary>
        /// <param name="no">�b�ԍ��i�����j</param>
        /// <returns>������K�p�����b��������</returns>
        //=========================================================================
        private String formatEpisodeNo(decimal no)
        {
            return no.ToString(Settings.Default.storyNoFormat);
        }
        // add yossiepon 20160924 end
		
	}

}
