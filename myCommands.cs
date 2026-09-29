// (C) Copyright 2025 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCADLispTool.MyCommands))]

namespace AutoCADLispTool
{
    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
    {
        // The form is modeless and owned by the AutoCAD main window, so a single
        // shared instance is reused instead of opening a new window per invocation.
        private static MainForm _mainForm;

        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        // Command to show MainForm in non-modal mode
        [CommandMethod("MyGroup", "OpenLispTool", "OpenLispToolLocal", CommandFlags.Modal)]
        public void OpenLispTool()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;

            try
            {
                if (_mainForm == null || _mainForm.IsDisposed)
                {
                    _mainForm = new MainForm();
                    _mainForm.FormClosed += (s, e) => _mainForm = null;

                    // Parents the form to the AutoCAD main window so it stays on top
                    // and receives keyboard input correctly.
                    AcadApp.ShowModelessDialog(_mainForm);
                    ed?.WriteMessage("\nLisp Tool window opened.");
                }
                else
                {
                    _mainForm.Activate();
                    ed?.WriteMessage("\nLisp Tool window is already open.");
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\nError opening Lisp Tool: {ex.Message}");
            }
        }
    }
}
