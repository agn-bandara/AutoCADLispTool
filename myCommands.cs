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
        [CommandMethod("MyGroup", "LispTool", "LispToolLocal", CommandFlags.Modal)]
        public void LispTool()
        {
            try
            {
                // Create and show the MainForm in non-modal mode
                MainForm mainForm = new MainForm();
                mainForm.Show(); // Non-modal - allows user to continue working in AutoCAD
                
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;
                    ed.WriteMessage("\nLisp Tool window opened.");
                }
            }
            catch (System.Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;
                    ed.WriteMessage($"\nError opening Lisp Tool: {ex.Message}");
                }
            }
        }
    }
}
