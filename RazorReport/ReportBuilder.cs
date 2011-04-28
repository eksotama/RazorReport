﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RazorReport {
    public class ReportBuilder<T> : IReportBuilder<T> {
        string name;
        string template;
        string masterTemplate;
        bool needsCompilation = true;
        string titleTag;
        IEngine<T> engine;

        private ReportBuilder () { }

        public static IReportBuilder<T> Create (string name) {
            return CreateWithEngineInstance (name, new Engine<T> ());
        }

        public static IReportBuilder<T> CreateWithEngineInstance (string name, IEngine<T> engine) {
            return new ReportBuilder<T> { name = name, engine = engine };
        }

        public IReportBuilder<T> WithTemplate (string templateString) {
            needsCompilation = templateString != template;
            template = templateString;
            return this;
        }

        public IReportBuilder<T> WithMasterTemplate (string templateString) {
            needsCompilation = masterTemplate != templateString;
            masterTemplate = templateString;
            return this;
        }

        public IReportBuilder<T> WithTemplateFromFileSystem (string templateFile) {
            return WithTemplate (TemplateFinder.GetTemplateFromFileSystem (templateFile));
        }

        public IReportBuilder<T> WithMasterTemplateFromFileSystem (string templateFile) {
            return WithMasterTemplate (TemplateFinder.GetTemplateFromFileSystem (templateFile));
        }

        public IReportBuilder<T> WithTemplateFromResource (string templateFile, Assembly assembly) {
            return WithTemplate (TemplateFinder.GetTemplateFromResource (templateFile, assembly));
        }

        public IReportBuilder<T> WithMasterTemplateFromResource (string templateFile, Assembly assembly) {
            return WithMasterTemplate (TemplateFinder.GetTemplateFromResource (templateFile, assembly));
        }

        public IReportBuilder<T> WithTitle (Expression<Func<T, string>> titleGenerator) {
            var mex = titleGenerator.Body as MemberExpression;
            if (mex != null) {
                titleTag = "@Model." + mex.Member.Name;
                needsCompilation = true;
                return this;
            }

            var mcex = titleGenerator.Body as MethodCallExpression;
            if (mcex != null && mcex.Arguments.Count == 0) {
                titleTag = "@Model." + mcex.Method.Name + "()";
                needsCompilation = true;
                return this;
            }

            throw new InvalidOperationException ("Invalid expression type passed.");
        }

        public string BuildHtml (T model) {
            if (needsCompilation) {
                engine.Compile (PrepareTemplate (), name);
                needsCompilation = false;
            }
            return engine.Run (model, name);
        }

        string PrepareTemplate () {
            var output = (string.IsNullOrEmpty (masterTemplate)) ? template : masterTemplate.Replace ("@@BODY", template);
            return output.Replace ("@@TITLE", titleTag);
        }
    }
}