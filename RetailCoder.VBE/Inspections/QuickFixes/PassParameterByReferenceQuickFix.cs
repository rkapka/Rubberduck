using Antlr4.Runtime;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Inspections.Resources;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Rubberduck.Parsing.Grammar.VBAParser;

namespace Rubberduck.Inspections.QuickFixes
{
    /// <summary>
    /// Encapsulates a code inspection quickfix that changes a ByVal parameter into an explicit ByRef parameter.
    /// </summary>
    public class PassParameterByReferenceQuickFix : QuickFixBase
    {
        private Declaration _target;
        private int _byValTokenProcLine;
        private int _byValIdentifierNameProcLine;

        public PassParameterByReferenceQuickFix(Declaration target, QualifiedSelection selection)
            : base(target.Context, selection, InspectionsUI.PassParameterByReferenceQuickFix)
        {
            _target = target;
            _byValTokenProcLine = 0;
            _byValIdentifierNameProcLine = 0;
        }

        public override void Fix()
        {
            string byValTargetString;
            string byValTokenReplacementString;
            string replacementString;

            var procLines = RetrieveProcedureLines();

            SetMemberLineValues(procLines);

            string moduleLineWithByValToken = procLines[_byValTokenProcLine - 1];

            if (_byValTokenProcLine == _byValIdentifierNameProcLine) 
            {
                //The replacement is based on the  (e.g. "ByVal identifierName")
                byValTargetString = Tokens.ByVal + " " + _target.IdentifierName;
                byValTokenReplacementString = BuildByRefParameter(byValTargetString);
                replacementString = moduleLineWithByValToken.Replace(byValTargetString, byValTokenReplacementString);
            }
            else
            {
                //if the token and identifier are on different lines, then  the target
                //string consists of the ByVal token and the LineContinuation token.
                //(e.g. the replacement is based on "ByVal   _". Spaces between tokens can vary)
                byValTargetString = GetUniqueTargetStringForByValAtEndOfLine(moduleLineWithByValToken);
                byValTokenReplacementString = BuildByRefParameter(byValTargetString);

                //avoid updating possible cases of ByVal followed by underscore-prefixed identifiers
                var index = moduleLineWithByValToken.LastIndexOf(byValTargetString);
                var firstPart = moduleLineWithByValToken.Substring(0, index);
                replacementString = firstPart + byValTokenReplacementString;
            }

            var module = Selection.QualifiedName.Component.CodeModule;
            module.ReplaceLine(RetrieveTheProcedureStartLine() + _byValTokenProcLine-1, replacementString);
        }
        private string[] RetrieveProcedureLines()
        {
            var moduleContent = Context.Start.InputStream.ToString();
            string[] newLine = { "\r\n" };
            var moduleLines = moduleContent.Split(newLine, StringSplitOptions.None);
            var procLines = new List<string>();
            var startIndex = RetrieveTheProcedureStartLine();
            var endIndex = RetrieveTheProcedureEndLine();
            for ( int idx = startIndex - 1; idx < endIndex; idx++)
            {
                procLines.Add(moduleLines[idx]);
            }
            return procLines.ToArray();
        }
        private int RetrieveTheProcedureStartLine()
        {
            var parserRuleCtxt = (ParserRuleContext)Context.Parent.Parent;
            return parserRuleCtxt.Start.Line;
        }
        private int RetrieveTheProcedureEndLine()
        {
            var parserRuleCtxt = (ParserRuleContext)Context.Parent.Parent;
            return parserRuleCtxt.Stop.Line;
        }
        private string BuildByRefParameter(string originalParameter)
        {
            var everythingAfterTheByValToken = originalParameter.Substring(Tokens.ByVal.Length);
            return Tokens.ByRef + everythingAfterTheByValToken;
        }
        private string GetUniqueTargetStringForByValAtEndOfLine(string procLineWithByValToken)
        {
            System.Diagnostics.Debug.Assert(procLineWithByValToken.Contains(Tokens.LineContinuation));

            var positionOfLineContinuation = procLineWithByValToken.LastIndexOf(Tokens.LineContinuation);
            var positionOfLastByValToken = procLineWithByValToken.LastIndexOf(Tokens.ByVal);
            return  procLineWithByValToken.Substring(positionOfLastByValToken, positionOfLineContinuation - positionOfLastByValToken + Tokens.LineContinuation.Length);
        }
        private void SetMemberLineValues(string[] procedureLines)
        {

            string line;
            bool byValTokenFound = false;
            bool byValIdentifierNameFound = false;
            for (int zbIndexByValLine = 0; !byValIdentifierNameFound && zbIndexByValLine < procedureLines.Length; zbIndexByValLine++)
            {
                line = procedureLines[zbIndexByValLine];
                if (line.Contains(Tokens.ByVal))
                {
                    _byValTokenProcLine = zbIndexByValLine + 1;
                    byValTokenFound = true;
                }
                if (byValTokenFound)
                {
                    int lineNum = GetIdentifierLineNumber(_target.IdentifierName);
                    if(lineNum > 0)
                    {
                        _byValIdentifierNameProcLine = lineNum;
                        byValIdentifierNameFound = true;
                    }
                    /*
                    if (line.Contains(_target.IdentifierName))
                    {
                        _byValIdentifierNameProcLine = zbIndexByValLine + 1;
                        byValIdentifierNameFound = true;
                    }
                    */
                }
            }

            System.Diagnostics.Debug.Assert(_byValTokenProcLine > 0);
            System.Diagnostics.Debug.Assert(_byValIdentifierNameProcLine > 0);
            return;
        }
        private int GetIdentifierLineNumber(string identifier)
        {
            var names = new List<string>();
            var test = (SubStmtContext)Context.Parent.Parent;
            var next = test.children;
            for (int idx = 0; idx < next.Count; idx++)
            {
                if (next[idx] is SubstmtContext)
                {
                    var child = (SubstmtContext)next[idx];
                    var arg = child.children;
                    for (int idx2 = 0; idx2 < arg.Count; idx2++)
                    {
                        if (arg[idx2] is ArgContext)
                        {
                            var asdf = (ArgContext)arg[idx2];
                            var kids = asdf.children;
                            for (int idx3 = 0; idx3 < kids.Count; idx3++)
                            {
                                var _start = (ParserRuleContext)kids[0];
                                var _stop = (ParserRuleContext)kids[kids.Count-1];
                                int startCol = _start.Start.Column;
                                int stopCol = _start.Stop.Column;

                                if (kids[idx3] is UnrestrictedIdentifierContext)
                                {
                                    var idRef = (UnrestrictedIdentifierContext)kids[idx3];
                                    var name = idRef.Start.Text;
                                    if (name.Equals(identifier))
                                    {
                                        int lineNum = idRef.Start.Line;
                                        return lineNum;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return -1;
        }
        private int GetIdentifierStartIndex(string identifier, out int stopIndex)
        {
            var names = new List<string>();
            var test = (SubStmtContext)Context.Parent.Parent;
            var next = test.children;
            for (int idx = 0; idx < next.Count; idx++)
            {
                if (next[idx] is SubstmtContext)
                {
                    var child = (SubstmtContext)next[idx];
                    var arg = child.children;
                    for (int idx2 = 0; idx2 < arg.Count; idx2++)
                    {
                        if (arg[idx2] is ArgContext)
                        {
                            var asdf = (ArgContext)arg[idx2];
                            var kids = asdf.children;
                            for (int idx3 = 0; idx3 < kids.Count; idx3++)
                            {
                                var _start = (ParserRuleContext)kids[0];
                                var _stop = (ParserRuleContext)kids[kids.Count - 1];
                                stopIndex = _start.Stop.Column;
                                return _start.Start.Column;

                                if (kids[idx3] is UnrestrictedIdentifierContext)
                                {
                                    var idRef = (UnrestrictedIdentifierContext)kids[idx3];
                                    var name = idRef.Start.Text;
                                    if (name.Equals(identifier))
                                    {
                                        int lineNum = idRef.Start.Line;
                                        return lineNum;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            stopIndex = -1;
            return -1;
        }
    }
}