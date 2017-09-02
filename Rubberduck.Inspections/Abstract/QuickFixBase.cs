using System;
using System.Collections.Generic;
using System.Linq;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.VBA;

namespace Rubberduck.Inspections.Abstract
{
    public abstract class QuickFixBase : IQuickFix
    {
        private HashSet<Type> _supportedInspections;
        public IReadOnlyCollection<Type> SupportedInspections => _supportedInspections.ToList();

        protected QuickFixBase(params Type[] inspections)
        {
            RegisterInspections(inspections);
        }

        public void RegisterInspections(params Type[] inspections)
        {
            if (!inspections.All(s => s.GetInterfaces().Any(a => a == typeof(IInspection))))
            {
                throw new ArgumentException($"Parameters must implement {nameof(IInspection)}", nameof(inspections));
            }

            _supportedInspections = inspections.ToHashSet();
        }

        public void RemoveInspections(params Type[] inspections)
        {
            _supportedInspections = _supportedInspections.Except(inspections).ToHashSet();
        }

        public abstract void Fix(IInspectionResult result);
        public abstract string Description(IInspectionResult result);

        public abstract bool CanFixInProcedure { get; }
        public abstract bool CanFixInModule { get; }
        public abstract bool CanFixInProject { get; }
    }
}