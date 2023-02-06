using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteCard.GamePlay
{
    public sealed class Expression
    {
        private readonly char[] Operators_ =
        {
            '+', '-', '*', '/', '(', ')'
        };
        
        private readonly char[] Buffer_;
        private readonly Stack<int> LastIndex_;
        private int Index_;
        private bool IsError_;

        public Expression(string exp)
        {
            Buffer_ = exp.Trim().ToCharArray();
            LastIndex_ = new Stack<int>();
            Index_ = 0;
            IsError_ = false;
        }

        public bool IsError()
        {
            return IsError_;
        }

        public float Calculate(AgentBase caster, AgentBase target)
        {
            var value = ParseExp(caster, target);
            
            if (IsError_)
            {
                return 0;
            }

            return value;
        }

        private float ParseExp(AgentBase caster, AgentBase target)
        {
            var value = ParseTerm(caster, target);

            while (!IsEnd())
            {
                var token = ReadToken();
                if (token == null)
                {
                    return value;
                }

                switch (token)
                {
                    case ")":
                        return value;
                    case "+":
                        var addValue = ParseTerm(caster, target);
                        value += addValue;
                        break;
                    case "-":
                        var subValue = ParseTerm(caster, target);
                        value -= subValue;
                        break;
                    default:
                        Rewind();
                        return value;
                }
            }

            return value;
        }

        private float ParseTerm(AgentBase caster, AgentBase target)
        {
            var value = ParseFactor(caster, target);

            while (!IsEnd())
            {
                var token = ReadToken();
                if (token == null)
                {
                    return value;
                }

                switch (token)
                {
                    case "*":
                        var mulValue = ParseFactor(caster, target);
                        value *= mulValue;
                        break;
                    case "/":
                        var divValue = ParseFactor(caster, target);
                        value /= divValue;
                        break;
                    default:
                        Rewind();
                        return value;
                }
            }

            return value;
        }

        private float ParseFactor(AgentBase caster, AgentBase target)
        {
            var token = ReadToken();
            if (token == null)
            {
                return 0;
            }

            if (token == "(")
            {
                return ParseExp(caster, target);
            }

            if (float.TryParse(token, out var value))
            {
                return value;
            }

            var val = VariableHandler.Instance.Calculate(caster, target, token);
            if (val == null)
            {
                IsError_ = true;
                return 0;
            }
            return val.Value;
        }

        private bool IsEnd()
        {
            return Index_ >= Buffer_.Length;
        }

        private void Rewind()
        {
            Index_ = LastIndex_.Pop();
        }
        
        private string? ReadToken()
        {
            var token = new StringBuilder();
            LastIndex_.Push(Index_);

            while (!IsEnd())
            {
                var ch = Buffer_[Index_];

                if (char.IsWhiteSpace(ch))
                {
                    Index_++;
                }
                else if (Operators_.Contains(ch))
                {
                    if (token.Length > 0)
                    {
                        return token.ToString();
                    }

                    Index_++;
                    return ch.ToString();
                }
                else
                {
                    token.Append(ch);
                    Index_++;
                }
            }

            if (token.Length > 0)
            {
                return token.ToString();
            }

            IsError_ = true;
            return null;
        }
    }
}