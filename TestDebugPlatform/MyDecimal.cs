using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArcTanX
{
    public class MyDecimal
    {
        #region 变量区

        /// <summary> 
        /// 存储介质 (342.61789 => 90 78 61 42 03)
        /// TODO 有时间给改成99999999 这个数量级的 
        /// 小数点 
        /// 符号
        /// </summary>
        public List<short> rawBytes;
        private static int radixPoint;
        private bool sign;
        protected static int actualMinorLength;


        /// <summary>
        /// 生成映射表 15->"15"   2->"02"   10->"10"
        /// </summary>
        private static String[] NumMap = new String[100];

        /// <summary>
        /// 确保只生成一次倒序映射表
        /// </summary>
        private static bool isMapGenerated = false;


        public int Length
        {
            get { return rawBytes.Count; }
        }

        public static int Precision
        {
            get { return radixPoint; }

            set { SetRadixPoint(value); }
        }

        public String RawNumber
        {
            get { return FromRawBytesToString(); }
            set { FromStringToRawBytes(value.ToString()); }
        }

        public bool Sign
        {
            get { return sign; }
            set { SetSign(value); }
        }

        #endregion


        #region 基本操作方法（构造函数，输入输出，调整参数）

        public MyDecimal()
        {
            rawBytes = new List<short>();
            sign = true;

            if (!isMapGenerated)
            {
                isMapGenerated = true;
                //NumMap[23]="3"+"2"="32"
                for (var i = 0; i < 100; i++)
                {

                    NumMap[i] = String.Format("{0:00}", i);
                }
            }
        }


        /// <summary>
        /// 对本身四舍五入的函数，仅针对小数精度为奇数的高精度数
        /// 考虑正负零三种情况
        /// 注意多次进位的情况
        /// </summary>
        public void OutputHalfAdjusting()
        {
            if (radixPoint % 2 == 1)
            {
                //“五入”——考虑进位
                if (rawBytes[0] % 10 >= 5)
                {
                    bool isLeftSideNine = false;
                    int flag = 0;
                    //如果要进位，要找到从左往右第一个不是9的位置，即循环进位的结束点
                    if (rawBytes[0] / 10 == 9)
                    {
                        for (var i = 1; i < rawBytes.Count; i++)
                        {
                            //整个是99的字节可以跳过
                            if (rawBytes[i] == 99)
                            {
                                flag++;
                            }

                            //如果出现“X9”这样的就找到了根源
                            else
                            {
                                if (rawBytes[i] % 10 == 9)
                                {
                                    isLeftSideNine = true;
                                    flag++;
                                }
                                break;
                            }
                        }

                        // "99.99 99 XX"这种情况
                        if (isLeftSideNine == false)
                        {
                            //如果全是“99”，就要加一位“10”了,诸如99.99 99 99
                            if (flag == rawBytes.Count - 1)
                            {
                                rawBytes.Add(1);
                                for (var i = 0; i < rawBytes.Count - 1; i++)
                                {
                                    rawBytes[i] = 0;
                                }
                            }

                            //诸如 99.99 99 23（倒序）
                            else
                            {
                                rawBytes[flag + 1] += 1;
                                for (var i = 0; i <= flag; i++)
                                {
                                    rawBytes[i] = 0;
                                }
                            }
                        }


                        // "99.99 99 X9 "的情况 flag=2
                        else
                        {
                            rawBytes[flag] = (short)(rawBytes[flag] + 1);

                            for (var i = 0; i < flag; i++)
                            {
                                rawBytes[i] = 0;
                            }
                        }

                    }


                    //正常一位的四舍五入
                    else
                    {
                        rawBytes[0] = (short)((rawBytes[0] / 10 + 1) * 10);
                    }

                }

                //“四舍”——根本不用管这种事情(其实这一步也不用做)
                else
                {
                    rawBytes[0] = (short)(rawBytes[0] - rawBytes[0] % 10);
                }
            }


        }

        public bool IsZero()
        {
            for (var i = 0; i < rawBytes.Count; i++)
            {
                if (rawBytes[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 【输出】从数据生成表示数字的字符串
        /// 先考虑四舍五入
        /// </summary>
        /// <returns> 返回高精度数对应的字符串 </returns>
        public String FromRawBytesToString()
        {
            String numStr = "";

            if (IsZero())
            {
                numStr = "0.";
                for (var i = 0; i < (Precision - 1) / 2; i++)
                {
                    numStr += "00";
                }
                return numStr;
            }

            //考虑奇数精度情况下的四舍五入
            if (radixPoint % 2 == 1)
            {
                OutputHalfAdjusting();
            }

            for (var i = rawBytes.Count - 1; i >= 0; i--)
            {
                if ((i + 1) * 2 == radixPoint || (i + 1) * 2 == radixPoint + 1)
                {
                    numStr += ".";
                }
                numStr += NumMap[rawBytes[i]];
            }

            //奇数精度下要把最后一位去掉
            if (radixPoint % 2 == 1)
            {
                numStr = numStr.Substring(0, numStr.Length - 1);
            }

            if (numStr[0] == '.')
            {
                numStr = '0' + numStr;
            }


            //删除多余的0
            while (numStr[0] == '0' && (numStr[1] >= '0' && numStr[1] <= '9'))
            {
                numStr = numStr.Substring(1);
            }


            if (sign == false)
            {
                numStr = "-" + numStr;
            }
            return numStr;
        }

        /// <summary>
        /// 【输入】从字符串生成高精度数
        /// </summary>
        /// <param name="numberString"> 希望输入的高精度数的字符串 </param>
        public void FromStringToRawBytes(String numberString)
        {
            rawBytes.Clear();
            if (Regex.IsMatch(numberString, @"^[0]*\.[0]*$"))
            {
                rawBytes.Add(0);
                for (var i = 0; i < (Precision - 1) / 2; i++)
                {
                    rawBytes.Add(0);
                }
                return;
            }

            //确认符号
            if (numberString[0] == '-')
            {
                numberString = numberString.Substring(1);
                sign = false;
            }
            else
            {
                sign = true;
            }

            //确认整数长度
            var strSection = numberString.Split('.');
            var lengthOfInteger = strSection[0].Length;
            var lengthOfFloat = strSection[1].Length;

            //如果给的小数位数比预期精度低，给他补零
            //否则就用截取（也不四舍五入了）

            //奇数位小数，最后补零
            if (lengthOfFloat % 2 == 1)
            {
                strSection[1] += '0';
                lengthOfFloat++;
            }

            //奇数位整数，补零
            if (lengthOfInteger % 2 == 1)
            {
                strSection[0] = '0' + strSection[0];
                lengthOfInteger++;
            }

            //确保小数精度够
            while (lengthOfFloat < Precision || lengthOfFloat % 2 != 0)
            {

                strSection[1] += '0';
                lengthOfFloat++;
            }

            //削减小数长度，倒序装载小数
            for (var i = (Precision + 1) / 2 - 1; i >= 0; i--)
            {
                var newFloatByte = (byte)((strSection[1][i * 2] - '0') * 10 + (strSection[1][i * 2 + 1] - '0'));
                rawBytes.Add(newFloatByte);
            }


            //倒序装载整数部分
            for (var i = lengthOfInteger / 2 - 1; i >= 0; i--)
            {
                var newIntegerByte = (byte)(((strSection[0][i * 2] - '0') * 10 + (strSection[0][i * 2 + 1] - '0')));
                rawBytes.Add(newIntegerByte);
            }
        }

        /// <summary>
        /// 重载ToString()
        /// </summary>
        /// <returns> 返回FromRawBytesToString()的字符串 </returns>
        public override string ToString()
        {
            return FromRawBytesToString();
        }

        /// <summary>
        /// 设置小数点位置
        /// </summary>
        /// <param name="number"> 表示希望保留多少位小数 </param>
        public static void SetRadixPoint(int number)
        {
            if (number >= 0 && number != radixPoint)
            {
                radixPoint = number;
                actualMinorLength = (radixPoint + 1) / 2;
            }
        }

        /// <summary>
        /// 设置符号(用于之后再调整符号的时候，一般应该用不到)
        /// </summary>
        /// <param name="isPositiveOrZero"></param>
        public void SetSign(bool isPositiveOrZero)
        {
            sign = isPositiveOrZero;
        }


        #endregion


        #region 深拷贝、自增自减、四则运算、幂函数、及比较函数（大多是运算符重载 + - ++ -- * / ^ > < == ）

        /// <summary>
        /// 深拷贝函数
        /// </summary>
        /// <returns></returns>
        public MyDecimal DeepCopy()
        {
            MyDecimal newOne = new MyDecimal();
            newOne.SetSign(this.Sign);
            newOne.rawBytes = new List<short>(rawBytes);
            return newOne;
        }

        /// <summary>
        /// double型数的隐式转换
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static implicit operator MyDecimal(double rhs)
        {
            MyDecimal result = new MyDecimal();
            result.RawNumber = String.Format("{0:F15}", rhs);
            return result;
        }

        /// <summary>
        /// float型数的隐式转换
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static implicit operator MyDecimal(float rhs)
        {
            MyDecimal result = new MyDecimal();
            result.RawNumber = String.Format("{0:F15}", rhs);
            return result;
        }

        /// <summary>
        /// int型数的隐式转换
        /// </summary>
        /// <param name="rhs">整型数 所以最后加".00"字段</param>
        /// <returns></returns>
        public static implicit operator MyDecimal(int rhs)
        {
            MyDecimal result = new MyDecimal();
            result.RawNumber = rhs.ToString() + ".00";
            return result;
        }

        /// <summary>
        /// 高精度加法
        /// 1.考虑符号（全负 全正都好办 如果一正一负，改成lhs-rhs 反之改成 rhs-lhs）
        /// 2.从低位到高位开始加
        /// 3.进位 （最后一位可能要增加）
        /// 4.符号 设置精度
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static MyDecimal operator +(MyDecimal lhs, MyDecimal rhs)
        {
            MyDecimal result = new MyDecimal();
            //符号异号的情况
            if (lhs.sign != rhs.sign)
            {
                if (lhs.sign == true)
                {
                    MyDecimal midResult = rhs.DeepCopy();
                    midResult.Sign = true;
                    result = lhs - midResult;
                }
                else
                {
                    MyDecimal midResult = lhs.DeepCopy();
                    midResult.Sign = true;
                    result = rhs - midResult;
                }
            }

            //符号同号的情况,倒序加,符号更新即可
            else
            {
                if (lhs.IsZero())
                {
                    return rhs.DeepCopy();
                }

                if (rhs.IsZero())
                {
                    return lhs.DeepCopy();
                }

                //拷贝非共有小数部分
                int deltaMinorLength = 0;
                int flag = 0;

                int count = actualMinorLength;
                short carry = 0;
                short sum = 0;

                //加小数部分
                for (var i = 0; i < count; i++)
                {
                    sum = (short)(lhs.rawBytes[i] + rhs.rawBytes[i + deltaMinorLength] + carry);
                    if (sum >= 100)
                    {
                        carry = 1;
                        sum = (short)(sum - 100);
                    }
                    else
                    {
                        carry = 0;
                    }
                    result.rawBytes.Add(sum);
                }


                //先加整数部分，再拷贝整数高位部分和进位
                int lhsMajorLength = lhs.rawBytes.Count - actualMinorLength;
                int rhsMajorLength = rhs.rawBytes.Count - actualMinorLength;

                if (lhsMajorLength > rhsMajorLength)
                {
                    //相加部分，保留进位
                    for (var i = 0; i < rhsMajorLength; i++)
                    {
                        sum =
                            (short)
                                (lhs.rawBytes[i + actualMinorLength] + rhs.rawBytes[i + actualMinorLength] +
                                 carry);
                        if (sum >= 100)
                        {
                            carry = 1;
                            sum = (short)(sum - 100);
                        }
                        else
                        {
                            carry = 0;
                        }
                        result.rawBytes.Add(sum);
                    }

                    //拷贝部分
                    short copyOne = 0;
                    for (var i = rhsMajorLength; i < lhsMajorLength; i++)
                    {
                        copyOne = (short)(lhs.rawBytes[i + actualMinorLength] + carry);
                        if (copyOne >= 100)
                        {
                            copyOne = (short)(copyOne - 100);
                            carry = 1;
                        }
                        else
                        {
                            carry = 0;
                        }
                        result.rawBytes.Add(copyOne);
                    }
                    if (carry > 0)
                    {
                        result.rawBytes.Add(1);
                    }
                }

                else if (lhsMajorLength < rhsMajorLength)
                {
                    //相加部分，保留进位
                    for (var i = 0; i < lhsMajorLength; i++)
                    {
                        sum =
                            (short)
                                (lhs.rawBytes[i + actualMinorLength] + rhs.rawBytes[i + actualMinorLength] +
                                 carry);
                        if (sum >= 100)
                        {
                            carry = 1;
                            sum = (short)(sum - 100);
                        }
                        else
                        {
                            carry = 0;
                        }
                        result.rawBytes.Add(sum);
                    }

                    //拷贝部分
                    short copyOne = 0;
                    for (var i = lhsMajorLength; i < rhsMajorLength; i++)
                    {
                        copyOne = (short)(rhs.rawBytes[i + actualMinorLength] + carry);
                        if (copyOne >= 100)
                        {
                            copyOne = (short)(copyOne - 100);
                            carry = 1;
                        }
                        else
                        {
                            carry = 0;
                        }
                        result.rawBytes.Add(copyOne);
                    }
                    if (carry > 0)
                    {
                        result.rawBytes.Add(1);
                    }
                }

                //二者整数位长相等的情况
                else
                {
                    //相加部分，保留进位
                    for (var i = 0; i < lhsMajorLength; i++)
                    {
                        sum =
                            (short)
                                (lhs.rawBytes[i + actualMinorLength] + rhs.rawBytes[i + actualMinorLength] +
                                 carry);
                        if (sum >= 100)
                        {
                            carry = 1;
                            sum = (short)(sum - 100);
                        }
                        else
                        {
                            carry = 0;
                        }
                        result.rawBytes.Add(sum);
                    }
                    //拷贝部分（考虑创建新一位情况）
                    if (carry > 0)
                    {
                        result.rawBytes.Add(01);
                    }
                }

                result.Sign = lhs.Sign;
            }
            return result;
        }

        /// <summary>
        /// 高精度减法
        /// 正负异号者归于加法类
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static MyDecimal operator -(MyDecimal lhs, MyDecimal rhs)
        {
            MyDecimal result = new MyDecimal();
            //异号相减=>同号相加
            if (lhs.sign != rhs.sign)
            {
                if (lhs.sign)
                {
                    MyDecimal midResult = rhs.DeepCopy();
                    midResult.Sign = true;
                    result = lhs + midResult;
                }
                else
                {
                    MyDecimal midResult = rhs.DeepCopy();
                    midResult.Sign = false;
                    result = lhs + midResult;
                }
            }

            //同号=>相减
            else
            {
                if (lhs.IsZero())
                {
                    MyDecimal minus_rhs = rhs.DeepCopy();
                    minus_rhs.Sign = false;
                    return minus_rhs;
                }
                if (rhs.IsZero())
                {
                    return lhs.DeepCopy();
                }
                //比较大小确认符号，后面都用较大较小值来计算
                MyDecimal BigOne = null;
                MyDecimal SmallOne = null;
                if (lhs.sign == true)
                {
                    if (lhs >= rhs)
                    {
                        BigOne = lhs;
                        SmallOne = rhs;
                        result.sign = true;
                    }

                    else
                    {
                        BigOne = rhs;
                        SmallOne = lhs;
                        result.sign = false;
                    }
                }

                else
                {
                    if (lhs < rhs)
                    {
                        BigOne = lhs;
                        SmallOne = rhs;
                        result.sign = false;
                    }

                    else
                    {
                        BigOne = rhs;
                        SmallOne = lhs;
                        result.sign = true;
                    }
                }
                //小数部分的相减
                short borrow = 0;
                short surplus;



                //减法
                for (var i = 0; i < actualMinorLength; i++)
                {
                    //被减数算上借位也比减数大，那就减
                    if (BigOne.rawBytes[i] >= SmallOne.rawBytes[i] + borrow)
                    {
                        surplus = (short)(BigOne.rawBytes[i] - SmallOne.rawBytes[i] - borrow);
                        borrow = 0;
                    }

                    //否则就借一百来减，然后向下借位
                    else
                    {
                        surplus = (short)(100 + BigOne.rawBytes[i] - SmallOne.rawBytes[i] - borrow);
                        borrow = 1;
                    }
                    result.rawBytes.Add(surplus);
                }




                //整数部分的算法（由于大数一定至少不比小数的整数部分短，所以前期可以安心地减）
                int bigIntegerLength = BigOne.rawBytes.Count - actualMinorLength;
                int smallIntegerLength = SmallOne.rawBytes.Count - actualMinorLength;

                //减法部分
                for (var i = 0; i < smallIntegerLength; i++)
                {
                    //被减数算上借位也比减数大，那就减
                    if (BigOne.rawBytes[i + actualMinorLength] >= SmallOne.rawBytes[i + actualMinorLength] + borrow)
                    {
                        surplus = (short)(BigOne.rawBytes[i + actualMinorLength] - SmallOne.rawBytes[i + actualMinorLength] - borrow);
                        borrow = 0;
                    }

                    //否则就借一百来减，然后向下借位
                    else
                    {
                        surplus = (short)(100 + BigOne.rawBytes[i + actualMinorLength] - SmallOne.rawBytes[i + actualMinorLength] - borrow);
                        borrow = 1;
                    }
                    result.rawBytes.Add(surplus);
                }

                if (bigIntegerLength > smallIntegerLength)
                {
                    //拷贝部分
                    short copyOne = 0;

                    for (var i = smallIntegerLength; i < bigIntegerLength; i++)
                    {
                        copyOne = (short)(BigOne.rawBytes[i + actualMinorLength] - borrow);
                        if (copyOne < 0)
                        {
                            copyOne = (short)(100 + copyOne);
                            borrow = 1;
                        }
                        else
                        {
                            borrow = 0;
                        }
                        result.rawBytes.Add(copyOne);
                    }
                }

                //削去整数最高位开始的一堆00 00 00...
                int count = result.rawBytes.Count;
                while (result.rawBytes[count - 1] == 0 && count > actualMinorLength + 1)
                {
                    result.rawBytes.RemoveAt(count - 1);
                    count--;
                }
            }


            return result;
        }

        //public static MyDecimal operator ++(MyDecimal lhs)
        //{
        //    MyDecimal result = lhs.DeepCopy();
        //    if (result.sign)
        //    {
        //        result.rawBytes[result.actualMinorLength] += 1;
        //        if (result.rawBytes[result.actualMinorLength] > 99)
        //        {
        //            result.rawBytes[result.actualMinorLength] -= 100;
        //            short carry = 1;
        //            for (var i = result.actualMinorLength + 1; i < result.rawBytes.Count; i++)
        //            {
        //                result.rawBytes[i] += carry;
        //                if (result.rawBytes[i] > 100)
        //                {
        //                    result.rawBytes[i] -= 100;
        //                    carry = 1;
        //                }
        //                else
        //                {
        //                    carry = 0;
        //                    break;
        //                }
        //            }
        //            //说明是一直进位，要加一位了
        //            if (carry == 1)
        //            {
        //                result.rawBytes.Add(1);
        //            }
        //        }
        //    }

        //    //可能要考虑变换符号的事情了
        //    else
        //    {
        //        //其他小数位都是0就可以
        //        if (result.rawBytes.Count - result.actualMinorLength == 1 && result.rawBytes[result.rawBytes.Count-1]==1)
        //        {

        //        }

        //        //一定可以变号
        //        if (result.rawBytes.Count - result.actualMinorLength == 1 &&
        //            result.rawBytes[result.rawBytes.Count - 1] == 0)
        //        {

        //        }

        //        result.rawBytes[result.actualMinorLength] -= 1;
        //        if (result.rawBytes[result.actualMinorLength] > 99)
        //        {
        //            result.rawBytes[result.actualMinorLength] -= 100;
        //            short carry = 1;
        //            for (var i = result.actualMinorLength + 1; i < result.rawBytes.Count; i++)
        //            {
        //                result.rawBytes[i] += carry;
        //                if (result.rawBytes[i] > 100)
        //                {
        //                    result.rawBytes[i] -= 100;
        //                    carry = 1;
        //                }
        //                else
        //                {
        //                    carry = 0;
        //                    break;
        //                }
        //            }
        //            //说明是一直进位，要加一位了
        //            if (carry == 1)
        //            {
        //                result.rawBytes.Add(1);
        //            }
        //        }
        //    }
        //    return result;
        //}

        //public static MyDecimal operator --(MyDecimal lhs)
        //{
        //    MyDecimal result = lhs.DeepCopy();
        //    return result;
        //}


        /// <summary>
        /// 确认符号（同号为正，异号为负）
        /// 确认精度（以大精度为准）
        /// 循环乘加，注意进位
        /// 对取精度位的下一位四舍五入
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static MyDecimal operator *(MyDecimal lhs, MyDecimal rhs)
        {
            MyDecimal result = new MyDecimal();

            if (lhs.IsZero())
            {
                return lhs.DeepCopy();
            }

            if (rhs.IsZero())
            {
                return rhs.DeepCopy();
            }

            result.sign = (lhs.sign == rhs.sign);

            int sum = 0;
            int carry = 0;
            for (var i = 0; i < lhs.rawBytes.Count + rhs.rawBytes.Count - 1; i++)
            {
                //上一进位和累乘
                sum = carry;
                for (var j = 0; j < lhs.rawBytes.Count; j++)
                {
                    if (i - j < 0)
                    {
                        break;
                    }
                    if (j < i + 1 - rhs.rawBytes.Count)
                    {
                        continue;
                    }
                    sum += (lhs.rawBytes[j] * rhs.rawBytes[i - j]);
                }

                //考虑下一进位
                if (sum > 99)
                {
                    carry = (sum / 100);
                    sum = (sum % 100);
                }
                else
                {
                    carry = 0;
                }
                result.rawBytes.Add((short)sum);
            }

            if (carry > 0)
            {
                result.rawBytes.Add((short)carry);
            }

            //小数精度四舍五入
            int toCutMinorLength = Math.Min(actualMinorLength, actualMinorLength);
            short decidedByte = result.rawBytes[toCutMinorLength - 1];
            result.rawBytes.RemoveRange(0, toCutMinorLength);

            if (decidedByte / 10 >= 5)
            {
                int flag = 0;
                while (flag != result.rawBytes.Count)
                {
                    if (result.rawBytes[flag] != 99)
                    {
                        result.rawBytes[flag] += 1;
                        break;
                    }

                    else
                    {
                        result.rawBytes[flag] = 0;
                    }
                    flag++;
                }

                if (flag == result.rawBytes.Count)
                {
                    result.rawBytes.Add(1);
                }
            }

            else
            {
                //Do Nothing
            }

            return result;
        }

        /// <summary>
        /// 高精度除法
        /// 符号相反就负，相同就正
        /// 精度等于较大的精度
        /// 每一次作差，试探性用首位乘系数比较大小（进行M+N+1次，其中M是整数部分，）
        /// 对取精度位的下一位四舍五入
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static MyDecimal operator /(MyDecimal lhs, MyDecimal rhs)
        {
            MyDecimal result = new MyDecimal();
            if (lhs.IsZero())
            {
                return lhs.DeepCopy();
            }

            //确定符号
            result.sign = (lhs.sign == rhs.sign);

            int lhsLength = lhs.rawBytes.Count;
            int rhsLength = rhs.rawBytes.Count;
            int lhsMinorLength = actualMinorLength;
            int rhsMinorLength = actualMinorLength;
            int lhsIntegerLength = lhsLength - lhsMinorLength;
            int rhsIntegerLength = rhsLength - rhsMinorLength;
            int CalulateLength = 0;
            int MaxMinorLength = Math.Max(lhsMinorLength, rhsMinorLength);

            CalulateLength = MaxMinorLength + 2;

            //装填剩余量(多加一位0在最开头，以便可以做100*surplus[index-1]+surplus[index]这样的操作)
            short[] surplus = new short[lhsLength + 1 + MaxMinorLength + rhsLength];
            surplus[0] = 0;
            for (var i = 1; i <= lhsLength; i++)
            {
                surplus[i] = lhs.rawBytes[lhsLength - i];
            }
            for (var i = lhsLength + 1; i < MaxMinorLength + rhsLength + lhsLength + 1; i++)
            {
                surplus[i] = 0;
            }

            //装填除数
            int zeroWidth = 0;
            int trimmedRhsLength = rhsLength;
            for (var i = rhsLength - 1; i >= 0; i--)
            {
                if (rhs.rawBytes[i] != 0)
                {
                    break;
                }
                else
                {
                    trimmedRhsLength--;
                }
            }

            //除数如果是小数，之前可能会有很多个00，那么要把小数点后的这种00 都删掉
            short[] divisor = new short[trimmedRhsLength];
            for (var i = 0; i < trimmedRhsLength; i++)
            {
                divisor[i] = rhs.rawBytes[trimmedRhsLength - 1 - i];
            }

            //装填试商对应的乘积
            bool isNewBitNeeded = false;
            short attemptFactor = 0;
            int index = 1;
            int lhsValidPosition = 0;
            int rhsValidPosition = 0;

            //对于除数大于被除数的情况，要在前面添加零
            //有效位：小数点后那一位是-1，往正加，往负减
            for (var i = lhsLength - 1; i >= 0; i--)
            {
                if (lhs.rawBytes[i] != 0)
                {
                    lhsValidPosition = i - lhsMinorLength;
                    break;
                }
            }
            for (var i = rhsLength - 1; i >= 0; i--)
            {
                if (rhs.rawBytes[i] != 0)
                {
                    rhsValidPosition = i - rhsMinorLength;
                    break;
                }
            }

            var deltaValidLength = lhsValidPosition - rhsValidPosition;
            // 因为除数比被除数大，所以补零
            while (deltaValidLength < 0)
            {
                result.rawBytes.Add(0);
                deltaValidLength++;
            }
            if (rhsLength != trimmedRhsLength)
            {
                CalulateLength += rhsLength - trimmedRhsLength;
            }

            if (CalulateLength - (rhsIntegerLength - lhsIntegerLength) > 0)
            {
                //试商相减运算(只要还没有达到所需的精度就一直算)
                while (index != CalulateLength - (rhsIntegerLength - lhsIntegerLength) + 1)
                {
                    isNewBitNeeded = false;
                    if (surplus[index - 1] == 0)
                    {
                        for (var i = 0; i < trimmedRhsLength; i++)
                        {
                            if (surplus[index + i] < divisor[i])
                            {
                                isNewBitNeeded = true;
                                break;
                            }
                            else if (surplus[index + i] > divisor[i])
                            {
                                break;
                            }
                        }
                        //需要再上一位的情况(直接填0，然后往后退一格)
                        if (isNewBitNeeded)
                        {
                            result.rawBytes.Add(0);
                            index++;
                        }

                        //这一位暂存已经比除数大了，就可以直接开始试商
                        //两位数存储用两位来试商绝对保证误差最多差一，考虑最坏情况：999900/10199 用9999/101=99 但是差也差不过98(98.03902342)
                        else
                        {
                            if (divisor.Count() == 1)
                            {
                                attemptFactor = (short)((surplus[index]) / (divisor[0]));
                            }
                            else
                            {
                                attemptFactor = (short)((surplus[index] * 100 + surplus[index + 1]) / (100 * divisor[0] + divisor[1]));
                            }

                            int carry = 0;
                            int borrow = 0;
                            //显然如果是1由于之前已经比较过了，那就可以直接赋上值
                            if (attemptFactor == 1)
                            {
                                for (var i = trimmedRhsLength - 1; i >= 0; i--)
                                {
                                    surplus[index + i] = (short)(surplus[index + i] - divisor[i] - borrow);
                                    if (surplus[index + i] < 0)
                                    {
                                        surplus[index + i] += 100;
                                        borrow = 1;
                                    }
                                    else
                                    {
                                        borrow = 0;
                                    }
                                }
                                result.rawBytes.Add(1);
                                index++;
                            }

                            //对于大于1的系数，可能后面还不够，于是要试减(先用attemptFactor-1来减，如果还有很多剩余，再减一遍divisor)
                            else
                            {
                                attemptFactor--;
                                for (var i = trimmedRhsLength - 1; i >= 0; i--)
                                {
                                    int sum = (attemptFactor * divisor[i] + carry);
                                    surplus[index + i] -= (short)(sum % 100 + borrow);
                                    carry = sum / 100;
                                    if (surplus[index + i] < 0)
                                    {
                                        borrow = 1;
                                        surplus[index + i] += 100;
                                    }
                                    else
                                    {
                                        borrow = 0;
                                    }
                                }
                                var isFinished = false;
                                for (var i = 0; i < trimmedRhsLength; i++)
                                {
                                    if (surplus[index + i] < divisor[i])
                                    {
                                        isFinished = true;
                                        break;
                                    }
                                    if (surplus[index + i] > divisor[i])
                                    {
                                        break;
                                    }
                                }
                                if (isFinished)
                                {
                                    result.rawBytes.Add(attemptFactor);
                                    index++;
                                }
                                //说明试商太保守，再减一遍divisor
                                else
                                {
                                    carry = 0;
                                    borrow = 0;
                                    for (var i = trimmedRhsLength - 1; i >= 0; i--)
                                    {
                                        surplus[index + i] = (short)(surplus[index + i] - divisor[i] - borrow);
                                        if (surplus[index + i] < 0)
                                        {
                                            surplus[index + i] += 100;
                                            borrow = 1;
                                        }
                                        else
                                        {
                                            borrow = 0;
                                        }
                                    }

                                    result.rawBytes.Add((short)(attemptFactor + 1));


                                    index++;
                                }
                            }
                        }
                    }

                    //对付那种上一位还有残存的那种
                    else
                    {
                        if (divisor.Count() == 1)
                        {
                            attemptFactor =
                            (short)
                                ((surplus[index - 1] * 100 + surplus[index]) /
                                 (divisor[0]));
                        }
                        else
                        {
                            attemptFactor =
                            (short)
                                ((surplus[index - 1] * 10000 + surplus[index] * 100 + surplus[index + 1]) /
                                 (100 * divisor[0] + divisor[1]));
                        }

                        int carry = 0;
                        int borrow = 0;

                        attemptFactor--;
                        for (var i = trimmedRhsLength - 1; i >= 0; i--)
                        {
                            int sum = (attemptFactor * divisor[i] + carry);
                            surplus[index + i] -= (short)(sum % 100 + borrow);
                            carry = sum / 100;
                            if (surplus[index + i] < 0)
                            {
                                borrow = 1;
                                surplus[index + i] += 100;
                            }
                            else
                            {
                                borrow = 0;
                            }
                        }
                        surplus[index - 1] -= (short)(carry + borrow);

                        //看是否减干净了
                        var isFinished = false;
                        if (surplus[index - 1] == 0)
                        {
                            for (var i = 0; i < trimmedRhsLength; i++)
                            {
                                if (surplus[index + i] < divisor[i])
                                {
                                    isFinished = true;
                                    break;
                                }
                                if (surplus[index + i] > divisor[i])
                                {
                                    break;
                                }
                            }
                        }

                        if (isFinished)
                        {
                            result.rawBytes.Add(attemptFactor);
                            index++;
                        }
                        //说明试商太保守，再减一遍divisor
                        else
                        {
                            carry = 0;
                            borrow = 0;
                            surplus[index - 1] = 0;
                            for (var i = trimmedRhsLength - 1; i >= 0; i--)
                            {
                                surplus[index + i] = (short)(surplus[index + i] - divisor[i] - borrow);
                                if (surplus[index + i] < 0)
                                {
                                    surplus[index + i] += 100;
                                    borrow = 1;
                                }
                                else
                                {
                                    borrow = 0;
                                }
                            }

                            result.rawBytes.Add((short)(attemptFactor + 1));
                            index++;
                        }
                    }
                }
            }

            //四舍五入部分
            result.rawBytes.Reverse();
            var decidedByte = result.rawBytes[0];
            result.rawBytes.RemoveAt(0);
            if (decidedByte / 10 >= 5)
            {
                int flag = 0;
                while (flag != result.rawBytes.Count)
                {
                    if (result.rawBytes[flag] != 99)
                    {
                        result.rawBytes[flag] += 1;
                        break;
                    }

                    else
                    {
                        result.rawBytes[flag] = 0;
                    }
                    flag++;
                }

                if (flag == result.rawBytes.Count)
                {
                    result.rawBytes.Add(1);
                }
            }

            //削减零
            int trimZeroPoint = result.rawBytes.Count - 1;
            while (result.rawBytes[trimZeroPoint] == 0 && trimZeroPoint > actualMinorLength)
            {
                result.rawBytes.RemoveAt(trimZeroPoint);
                trimZeroPoint--;
            }

            return result;
        }

        /// <summary>
        /// 判断两个数是否在精度要求上相等
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool IsEqualAlmostly(MyDecimal left, MyDecimal right, int ExpectedPrecision)
        {
            bool isAllZero = true;
            //整数部分不一样长，直接判否
            if (left.rawBytes.Count != right.rawBytes.Count)
            {
                return false;
            }

            //整数部分不一样，直接判否
            var count = left.rawBytes.Count;
            var minorLength = (Precision + 1) / 2;
            for (var i = count - 1; i >= minorLength; i--)
            {
                if (left.rawBytes[i] != right.rawBytes[i])
                {
                    return false;
                }
                if (left.rawBytes[i] != 0)
                {
                    isAllZero = false;
                }
            }



            //小数部分前多少位不一样，直接判否
            //不考虑四舍五入问题
            if (ExpectedPrecision % 2 == 0)
            {
                for (var i = minorLength - 1; i >= minorLength - (ExpectedPrecision + 1) / 2; i--)
                {
                    if (left.rawBytes[i] != right.rawBytes[i])
                    {
                        return false;
                    }
                    if (left.rawBytes[i] != 0)
                    {
                        isAllZero = false;
                    }
                }
            }

            else
            {
                for (var i = minorLength - 1; i >= minorLength - (ExpectedPrecision + 1) / 2 + 1; i--)
                {
                    if (left.rawBytes[i] != right.rawBytes[i])
                    {
                        return false;
                    }
                    if (left.rawBytes[i] != 0)
                    {
                        isAllZero = false;
                    }
                }
                if (left.rawBytes[minorLength - (ExpectedPrecision + 1) / 2] / 10 !=
                    right.rawBytes[minorLength - (ExpectedPrecision + 1) / 2] / 10)
                {
                    return false;
                }
                if (left.rawBytes[minorLength - (ExpectedPrecision + 1) / 2] / 10 != 0)
                {
                    isAllZero = false;
                }
            }


            //如果前多少位并不是全零=>

            if (isAllZero)
            {
                return true;
            }
            else
            {
                if (left.Sign != right.Sign)
                {
                    return false;
                }
            }
            return true;

        }

        /// <summary>
        /// 比大小函数
        /// 首先看符号位
        /// 然后看整数位长度
        /// 相同比较整数大小
        /// 然后看小数位长度
        /// 相同比较小数大小（这里不考虑精度）
        /// 然后给出判断
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <(MyDecimal lhs, MyDecimal rhs)
        {
            //符号比较
            if (lhs.sign != rhs.sign)
            {
                if (lhs.sign == false)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }


            else
            {
                if (rhs == lhs)
                {
                    return false;
                }

                bool isNegative = !lhs.sign;

                //比较整数位长度
                int lhsIntegerLength = lhs.rawBytes.Count - actualMinorLength;
                int rhsIntegerLength = rhs.rawBytes.Count - actualMinorLength;
                if (lhsIntegerLength < rhsIntegerLength)
                {
                    return (isNegative ^ true);
                }

                else if (lhsIntegerLength > rhsIntegerLength)
                {
                    return (isNegative ^ false);
                }

                //位数相等，比较整数大小
                else
                {
                    for (var i = lhsIntegerLength - 1; i >= 0; i--)
                    {
                        if (lhs.rawBytes[i + actualMinorLength] < rhs.rawBytes[i + actualMinorLength])
                        {
                            return (isNegative ^ true);
                        }
                        else if (lhs.rawBytes[i + actualMinorLength] > rhs.rawBytes[i + actualMinorLength])
                        {
                            return (isNegative ^ false);
                        }
                    }

                    //整数也一样，那就比较小数
                    int deltaMinorLength = Math.Abs(actualMinorLength - actualMinorLength);
                    if (actualMinorLength <= actualMinorLength)
                    {
                        for (var i = actualMinorLength - 1; i >= 0; i--)
                        {
                            if (lhs.rawBytes[i] < rhs.rawBytes[i + deltaMinorLength])
                            {
                                return (isNegative ^ true);
                            }
                            else if (lhs.rawBytes[i] > rhs.rawBytes[i + deltaMinorLength])
                            {
                                return (isNegative ^ false);
                            }
                        }

                        //共有小数一样长，那lhs更短，所以正的lhs更小，负的lhs更大
                        return (isNegative ^ true);
                    }

                    else
                    {
                        for (var i = actualMinorLength - 1; i >= 0; i--)
                        {
                            if (lhs.rawBytes[i + deltaMinorLength] < rhs.rawBytes[i])
                            {
                                return (isNegative ^ true);
                            }
                            else if (lhs.rawBytes[i + deltaMinorLength] > rhs.rawBytes[i])
                            {
                                return (isNegative ^ false);
                            }
                        }

                        //共有小数一样长，那lhs更长，所以正的lhs更大，负的lhs更小
                        return (isNegative ^ false);
                    }
                }
            }
        }

        /// <summary>
        /// 简单的逻辑推导
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >(MyDecimal lhs, MyDecimal rhs)
        {
            if (lhs == rhs)
            {
                return false;
            }
            else
            {
                if (lhs < rhs)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 判等函数
        /// 首先符号
        /// 其次整数位数
        /// 其次小数位数（忽略最后为0的部分，算出一个等效小数位数）
        /// 遍历所有的数值
        /// 嗯，看来是相等了
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator ==(MyDecimal lhs, MyDecimal rhs)
        {
            //符号
            if (lhs.sign != rhs.sign)
            {
                return false;
            }

            else
            {
                //整数长度
                int lhsIntegerLength = lhs.rawBytes.Count - actualMinorLength;
                int rhsIntegerLength = rhs.rawBytes.Count - actualMinorLength;
                if (lhsIntegerLength != rhsIntegerLength)
                {
                    return false;
                }

                else
                {
                    //算出有效(非零)小数位数
                    int lhsZeroLength = 0;
                    int rhsZeroLength = 0;
                    for (var i = 0; i < actualMinorLength; i++)
                    {
                        if (lhs.rawBytes[i] != 0)
                        {
                            lhsZeroLength = i;
                            break;
                        }
                    }

                    for (var i = 0; i < actualMinorLength; i++)
                    {
                        if (rhs.rawBytes[i] != 0)
                        {
                            rhsZeroLength = i;
                            break;
                        }
                    }

                    if (actualMinorLength - lhsZeroLength != actualMinorLength - rhsZeroLength)
                    {
                        return false;
                    }

                    else
                    {
                        //要比较每一位了
                        int iterLength = lhs.rawBytes.Count - lhsZeroLength;
                        for (var i = 0; i < iterLength; i++)
                        {
                            if (lhs.rawBytes[i + lhsZeroLength] != rhs.rawBytes[i + rhsZeroLength])
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            //苦尽甘来~两个数终于相等了呢~
            return true;
        }

        /// <summary>
        /// 这个。。。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator !=(MyDecimal lhs, MyDecimal rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// 这个。。。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >=(MyDecimal lhs, MyDecimal rhs)
        {
            return !(lhs < rhs);
        }

        /// <summary>
        /// 这个。。。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <=(MyDecimal lhs, MyDecimal rhs)
        {
            return !(lhs > rhs);
        }


        //public static MyDecimal operator ^(MyDecimal lhs, MyDecimal rhs)
        //{
        //    MyDecimal result = new MyDecimal();
        //    return result;
        //}

        #endregion
    }
}
