// The only file that names the CAD vendor's .NET namespaces. The rest of src/AutoPDF.Cad is written against
// these imports and aliases, and each host project compiles it against its own vendor SDK:
//
//   AutoPDF.AutoCAD        AutoCAD 2021-2024 (R24, .NET Framework 4.8)   Autodesk.AutoCAD.*   symbol ACAD
//   AutoPDF.AutoCAD.R25    AutoCAD 2025-2026 (R25, .NET 8)               Autodesk.AutoCAD.*   symbol ACAD
//   AutoPDF.ZWCAD.20xx     ZWCAD 2024 / 2025 / 2026 (.NET Framework)     ZwSoft.ZwCAD.*       symbol ZWCAD
//
// AutoCAD assemblies are never loaded into ZWCAD (or the other way round): every host gets its own build.
// The vendor Runtime namespaces are not imported globally because their Exception type would clash with
// System.Exception (and GraphicsInterface has types such as Polyline): the few files that need them import
// them locally with the same #if.

#if ZWCAD
global using ZwSoft.ZwCAD.ApplicationServices;
global using ZwSoft.ZwCAD.DatabaseServices;
global using ZwSoft.ZwCAD.EditorInput;
global using ZwSoft.ZwCAD.Geometry;
global using ZwSoft.ZwCAD.PlottingServices;
global using CadApp = ZwSoft.ZwCAD.ApplicationServices.Application;
global using CadCoreApp = ZwSoft.ZwCAD.ApplicationServices.Application;
global using CadException = ZwSoft.ZwCAD.Runtime.Exception;
global using CadPolyline = ZwSoft.ZwCAD.DatabaseServices.Polyline;
global using DbPlotType = ZwSoft.ZwCAD.DatabaseServices.PlotType;
#else
global using Autodesk.AutoCAD.ApplicationServices;
global using Autodesk.AutoCAD.DatabaseServices;
global using Autodesk.AutoCAD.EditorInput;
global using Autodesk.AutoCAD.Geometry;
global using Autodesk.AutoCAD.PlottingServices;
global using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
global using CadCoreApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
global using CadException = Autodesk.AutoCAD.Runtime.Exception;
global using CadPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
global using DbPlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;
#endif
