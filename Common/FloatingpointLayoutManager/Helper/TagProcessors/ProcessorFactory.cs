using Kitsune.Language.Helper;
using Kitsune.Language.Models;
using System;
using System.Collections.Generic;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class ProcessorFactory
    {
        private Dictionary<string, TagProcessor> ProcessorDictionary;
        private TagProcessor noOpProcessor;
        private static ProcessorFactory _instance;

        private ProcessorFactory()
        {
            InitializeFactory();
        }

        public static ProcessorFactory GetProcessorFactory()
        {
            if (_instance == null)
            {
                _instance = new ProcessorFactory();
            }
            return _instance;
        }

        private void InitializeFactory() {
            ProcessorDictionary = new Dictionary<string, TagProcessor>();
            ProcessorDictionary.Add(LanguageAttributes.KObject.GetDescription().ToLower(), new KObjectProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KDL.GetDescription().ToLower(), new KDLProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KRepeat.GetDescription().ToLower(), new KRepeatProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KScript.GetDescription().ToLower(), new KScriptProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KShow.GetDescription().ToLower(), new KShowProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KHide.GetDescription().ToLower(), new KHideProcessor());
            ProcessorDictionary.Add(LanguageAttributes.KPayAmount.GetDescription().ToLower(), new KPayAmountProcessor());
            noOpProcessor = new NoOp();
        }

        public TagProcessor GetProcessor(string ktag)
        {
            if (null == ProcessorDictionary)
            {
                InitializeFactory();
            }

            if (ProcessorDictionary.ContainsKey(ktag))
            {
                return ProcessorDictionary[ktag];
            }
            else
            {
                return noOpProcessor;
            }
        }
    }
}
