//=========================================================================
///	<summary>
///		AnimeProgram���X�g�̃\�[�g�R���y�A�N���X
///	</summary>
/// <remarks>
/// </remarks>
/// <history>2019/11/24 �V�K�쐬</history>
//=========================================================================
using System;
using System.Collections.Generic;
using magicAnime.Properties;

namespace magicAnime
{
    //=========================================================================
    ///	<summary>
    ///		AnimeProgram���X�g�̃\�[�g�R���y�A���N���X
    ///	</summary>
    /// <remarks>
    /// </remarks>
    /// <history>2019/11/24 �V�K�쐬</history>
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
        ///		�R���X�g���N�^
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 �V�K�쐬</history>
        //=========================================================================
        public AnimeSortMod(Order order, OrderOption option) : base(order, option)
        {
        }

        //=========================================================================
        ///	<summary>
        ///		AnimeProgram��O�����X�L�������ă\�[�g��\�l�����肷��
        ///	</summary>
        ///	<param name="p">�ԑg</param>
        ///	<param name="baseTime">�\�[�g�����</param>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 �V�K�쐬</history>
        //=========================================================================
        public override void PreScan(AnimeProgram p, DateTime baseTime)
        {
            p.mSortRepresentative = DateTime.MaxValue;

            // �b����0�̏ꍇ�͑O�ɉ�
            if (p.StoryCount == 0)
            {
                p.mSortRepresentative = DateTime.MinValue;
                return;
            }

            switch (order)
            {
                // �j���Ǝ����̕��ς���Ƀ\�[�g
                case Order.DayOfWeek:
                    PreScanDayOfWeek(p, baseTime);
                    break;
                // ����̕��������Ń\�[�g
                default:
                case Order.NextOnair:
                    PreScanNextOnAir(p, baseTime);
                    break;
            }
        }

        private void PreScanDayOfWeek(AnimeProgram p, DateTime baseTime)
        {
            //--------------------------
            // �\�[�g������ȍ~�̕�������擾
            //�i�����J�n������������ȍ~�ł���Ί܂߂�j
            //--------------------------
            List<AnimeEpisode> normals = p.Episodes.FindAll(e => e.HasPlan && e.StartDateTime >= baseTime);

            // �ʏ�񂪑��݂����
            if (normals.Count > 0)
            {
                // �ŐV13�b�݂̂Ŕ��f����ꍇ
                if ((orderOption & OrderOption.Limit1CoursOption) != 0)
                {
                    // �ʏ��14�b�ȏ゠��ΐ擪�̗]���ȕ�������폜
                    if (normals.Count > 13)
                    {
                        normals.Sort(comp);
                        normals.RemoveRange(0, normals.Count - 13);
                    }
                }

                // �ʏ��݂̂ŗj���Ǝ����̕��ς��Ƃ�
                long ticks = 0;
                normals.ForEach(e => ticks += ModuloDateTimeOfWeek(e));
                p.mSortRepresentative = new DateTime(ticks / normals.Count * TICKS_OF_SECOND);
            }
            // �����I�����𖖔��ɉ�
            else
            {
                AnimeEpisode last = LastEpicode(p);

                // �����I�����𖖔��ɉ�
                if ((orderOption & OrderOption.LastOrder) != 0)
                {
                    // �����I��������ԍŌ�ɕ������ꂽ���ԂŖ����ɕ��ׂ�
                    p.mSortRepresentative = last.StartDateTime;
                    return;
                }
                // ��ԍŌ�ɕ������ꂽ�j���Ǝ����ŕ��ׂ�
                else
                {
                    p.mSortRepresentative = new DateTime(ModuloDateTimeOfWeek(last) * TICKS_OF_SECOND);
                }
            }
        }

        private void PreScanNextOnAir(AnimeProgram p, DateTime baseTime)
        {
            // ���̕��������������
            AnimeEpisode ep;
            AnimeProgram.NextEpisode epType = p.GetNextEpisode(baseTime, out ep);

            // ���̕����񂪑��݂��Ȃ����
            if (epType != AnimeProgram.NextEpisode.NextDecided)
            {
                // ��ԍŌ�ɕ������ꂽ�������I������
                ep = LastEpicode(p);

                // �����I�����𖖔��ɉ�
                if ((orderOption & OrderOption.LastOrder) != 0)
                {
                    // �����I��������ԍŌ�ɕ������ꂽ���ԂŖ����ɕ��ׂ�
                    // �����J�n������100�N�𑫂������̂��\�l�Ƃ���
                    p.mSortRepresentative = ep.StartDateTime.AddYears(100);
                    return;
                }
            }

            // �擾����������̕����J�n�������\�[�g��\�l�Ƃ���
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
        ///		AnimeProgram�̕������ԃR���y�A
        ///	</summary>
        /// <remarks>
        /// </remarks>
        /// <history>2019/11/24 �V�K�쐬</history>
        //=========================================================================
        public override int Compare(AnimeProgram x, AnimeProgram y)
        {
            return x.mSortRepresentative.CompareTo(y.mSortRepresentative);
        }
    }
}
