using System;
using System.IO;
using AseImport.Parsing;
using UnityEngine;

namespace AseImport.Importer
{
  public static class MetaLayerParser
  {
    class TokenType
    {
      readonly string id;

      public TokenType(string id)
      {
        this.id = id;
      }

      public override string ToString()
      {
        return id;
      }
    }

    static readonly TokenType tknString = new TokenType("string");
    static readonly TokenType tknNumber = new TokenType("number");
    static readonly TokenType tknId = new TokenType("id");
    static readonly TokenType tknLeft = new TokenType("left_bracket");
    static readonly TokenType tknRight = new TokenType("right_bracket");
    static readonly TokenType tknComma = new TokenType("comma");
    static readonly TokenType tknSpace = new TokenType("space");
    static readonly TokenType tknBool = new TokenType("bool");

    static readonly TokenDefinition[] defs;

    static MetaLayerParser()
    {
      defs = new []
      {
        new TokenDefinition(@"""[^""]*""", tknString),
        new TokenDefinition(@"([-+]?\d+\.\d+([eE][-+]?\d+)?)|([-+]?\d+)", tknNumber),
        new TokenDefinition(@"(true)|(false)", tknBool),
        new TokenDefinition(@"[a-zA-Z0-9_\-]+", tknId),
        new TokenDefinition(@"\,", tknComma),
        new TokenDefinition(@"\(", tknLeft),
        new TokenDefinition(@"\)", tknRight),
        new TokenDefinition(@"\s*", tknSpace),
      };
    }

    public static void Parse(ParameterBase layer)
    {
      if (!layer.Name.StartsWith("@"))
      {
        return;
      }

      var reader = new StringReader(layer.Name.Substring(1));
      var lexer = new Lexer(reader, defs);
      ParseInternal(lexer, layer);
    }

    static void ParseInternal(Lexer lexer, ParameterBase layer)
    {
      layer.ActionName = _Expect(lexer, tknId);

      if (!_SkipSpaces(lexer))
      {
        return;
      }

      if (lexer.Token != tknLeft)
      {
        _ErrorUnexpected(lexer, tknLeft);
      }

      while (true)
      {
        if (!_SkipSpaces(lexer))
        {
          _ErrorEOF(lexer, tknRight, tknNumber, tknString);
        }

        bool isParam = false;
        if (lexer.Token == tknString)
        {
          var stringValue = lexer.TokenContents.Substring(1, lexer.TokenContents.Length - 2);
          layer.AddParameter(new LayerParam(stringValue));
          isParam = true;
        }
        else if (lexer.Token == tknNumber)
        {
          layer.AddParameter(new LayerParam(double.Parse(lexer.TokenContents)));
          isParam = true;
        }
        else if (lexer.Token == tknBool)
        {
          layer.AddParameter(new LayerParam(bool.Parse(lexer.TokenContents)));
          isParam = true;
        }
        else if (lexer.Token == tknRight)
        {
          break;
        }
        else
        {
          _ErrorUnexpected(lexer, tknRight, tknNumber, tknString, tknBool);
        }

        if (isParam)
        {
          if (!_SkipSpaces(lexer))
          {
            _ErrorEOF(lexer, tknComma, tknRight);
          }

          if (lexer.Token == tknRight)
          {
            break;
          }

          if (lexer.Token != tknComma)
          {
            _ErrorUnexpected(lexer, tknComma, tknRight);
          }
        }
      }

      if (_SkipSpaces(lexer))
      {
        Debug.LogWarning("Invalid content after layer definition finished: " + lexer.Token + "/" + lexer.TokenContents);
      }
    }

    static bool _SkipSpaces(Lexer lexer)
    {
      while (true)
      {
        var hasMore = lexer.Next();
        if (!hasMore || lexer.Token != tknSpace)
        {
          return hasMore;
        }
      }
    }

    static string _Expect(Lexer lexer, TokenType tokenType)
    {
      var hasMore = _SkipSpaces(lexer);

      if (!hasMore)
      {
        throw _ErrorEOF(lexer, tokenType);
      }

      if (lexer.Token != tokenType)
      {
        _ErrorUnexpected(lexer, tokenType);
      }

      return lexer.TokenContents;
    }

    static Exception _ErrorEOF(Lexer lexer, params TokenType[] expected)
    {
      throw _Error(lexer, $"Expected {_TokenTypeStr(expected)}, found EOF");
    }

    static void _ErrorUnexpected(Lexer lexer, params TokenType[] expected)
    {
      throw _Error(lexer, $"Expected {_TokenTypeStr(expected)}, found {lexer.Token}:{lexer.TokenContents}");
    }

    static string _TokenTypeStr(TokenType[] expected)
    {
      string typeStr = "";
      for (int i = 0; i < expected.Length; ++i)
      {
        typeStr += expected[i];
        if (i != expected.Length - 1)
        {
          typeStr += " or ";
        }
        else
        {
          typeStr += " ";
        }
      }

      return typeStr;
    }

    static Exception _Error(Lexer lexer, string msg)
    {
      throw new Exception(msg);
    }
  }
}