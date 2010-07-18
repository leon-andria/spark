using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.Generic;
using Spark.Parser.Markup;
using System;
using System.Linq;
using SparkSense.Parsing;
using Spark.Compiler;

namespace SparkSense.StatementCompletion.CompletionSets
{
    public enum AttributeContexts
    {
        Name, Value
    }
    public class AttributeCompletionSet : CompletionSetFactory
    {
        private List<Completion> _completionList;

        public AttributeContexts AttributeContext
        {
            get
            {
                if (SparkSyntax.IsPositionInAttributeValue(CurrentContent, _triggerPoint))
                    return AttributeContexts.Value;
                return AttributeContexts.Name;
            }
        }
        
        protected override IList<Completion> GetCompletionSetForNodeAndContext()
        {
            if (_completionList != null) return _completionList;
            _completionList = new List<Completion>();

            switch (AttributeContext)
            {
                case AttributeContexts.Name:
                    _completionList.AddRange(GetForSpecialNodes());
                    break;
                case AttributeContexts.Value:
                    _completionList.AddRange(GetForAttributeValue());
                    break;
                default:
                    break;
            }
            return _completionList.SortAlphabetically();
        }

        private static bool IsPositionBetweenEmptyQuotes()
        {
            if (CurrentContent.Length == _triggerPoint) return false;

            var quoteAfterCaret =
                CurrentContent[_triggerPoint] == Constants.DOUBLE_QUOTE ||
                CurrentContent[_triggerPoint] == Constants.SINGLE_QUOTE;

            var quoteBeforeCaret =
                CurrentContent[_triggerPoint - 1] == Constants.DOUBLE_QUOTE ||
                CurrentContent[_triggerPoint - 1] == Constants.SINGLE_QUOTE;

            return quoteBeforeCaret && quoteAfterCaret;
        }

        private IEnumerable<Completion> GetForSpecialNodes()
        {
            var attributesForSpecialNode = new List<Completion>();
            Node specialNode;
            if (SparkSyntax.IsSparkNode(CurrentNode, out specialNode))
            {
                var knownAttributesForNode = GetKnownAttributesForSpecialNode((SpecialNode)specialNode);

                foreach (var attribute in ((SpecialNode)specialNode).Element.Attributes)
                    if (knownAttributesForNode.Exists(a => a == attribute.Name))
                        knownAttributesForNode.Remove(attribute.Name);

                knownAttributesForNode.ForEach(attribute => attributesForSpecialNode.Add(
                    new Completion(
                        attribute,
                        String.Format("{0}=\"\"", attribute),
                        String.Format("'{0}' attribute for '{1}' element", attribute, ((SpecialNode)specialNode).Element.Name),
                        GetIcon(Constants.ICON_SparkAttribute), null)));
            }

            return attributesForSpecialNode;
        }

        private IEnumerable<Completion> GetForAttributeValue()
        {
            var attributeValues = new List<Completion>();

            if (IsPositionBetweenEmptyQuotes())
            {
                var chunk = SparkSyntax.ParseContextChunk(CurrentContent, _triggerPoint);

                if (chunk == typeof(ContentChunk))
                    attributeValues.AddRange(GetContentNames());
                else if (chunk == typeof(ConditionalChunk))
                    attributeValues.AddRange(GetVariables());
                else if (chunk == typeof(UseMasterChunk))
                    attributeValues.AddRange(GetPossibleMasterNames());
            }
            else if (CurrentContent[_triggerPoint - 1] == Constants.SPACE)
            {

            }
            return attributeValues;
        }
     
        private static List<string> GetKnownAttributesForSpecialNode(SpecialNode node)
        {
            var allKnown = new Dictionary<string, List<string>>
            {
                {"var", new List<string>{"type"}},
                {"def", new List<string>{"type"}},
                {"default", new List<string>{"type"}},
                {"global", new List<string>{"type"}},
                {"viewdata", new List<string>{"default","model"}},
                {"set", new List<string>()},
                {"for", new List<string>{"each"}},
                {"test", new List<string>{"condition", "if", "once"}},
                {"if", new List<string>{"condition", "once"}},
                {"else", new List<string>{"if"}},
                {"elseif", new List<string>{"condition"}},
                {"content", new List<string>{"add","def", "name", "set", "var"}},
                {"use", new List<string>{"assembly", "content", "file", "import", "master", "namespace", "pageBaseType" }},
                {"macro", new List<string>{"name"}},
                {"render", new List<string>{"partial", "section"}},
                {"section", new List<string>{"name"}},
                {"cache", new List<string>{"expires", "key", "signal"}}
            };

            List<string> knownAttributes;

            return allKnown.TryGetValue(node.Element.Name, out knownAttributes)
                ? knownAttributes
                : new List<string>();
        }

        private string GetInsertionTextForContext(string item)
        {
            if (AttributeContext == AttributeContexts.Name)
                return String.Format("{0}=\"\"", item);
            return item;
        }

        private IEnumerable<Completion> GetVariables()
        {
            var variables = new List<Completion>();
            if (_viewExplorer == null) return variables;

            _viewExplorer.GetGlobalVariables().ToList().ForEach(
                variable => variables.Add(
                    new Completion(variable, String.Format("{0}=\"\"", variable), string.Format("Global Variable: '{0}'", variable), GetIcon(Constants.ICON_SparkGlobalVariable), null)));

            _viewExplorer.GetLocalVariables().ToList().ForEach(
                variable => variables.Add(
                    new Completion(variable, GetInsertionTextForContext(variable), string.Format("Local Variable: '{0}'", variable), GetIcon(Constants.ICON_SparkLocalVariable), null)));

            return variables;
        }

        private IEnumerable<Completion> GetContentNames()
        {
            var contentNames = new List<Completion>();
            if (_viewExplorer == null) return contentNames;

            _viewExplorer.GetContentNames().ToList().ForEach(
                contentName => contentNames.Add(
                    new Completion(contentName, contentName, string.Format("Content Name: '{0}'", contentName), GetIcon(Constants.ICON_SparkContentName), null)));
            return contentNames;
        }

        private IEnumerable<Completion> GetPossibleMasterNames()
        {
            var possibleMasters = new List<Completion>();
            if (_viewExplorer == null) return possibleMasters;

            _viewExplorer.GetPossibleMasterLayouts().ToList().ForEach(
                possibleMaster => possibleMasters.Add(
                    new Completion(possibleMaster, possibleMaster, string.Format("Possible Master: '{0}'", possibleMaster), GetIcon(Constants.ICON_SparkMacroParameter), null)));
            return possibleMasters;
        }
    }
}