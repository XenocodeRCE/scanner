using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Scanner {
    public partial class Form1 : Form {

        public string DirectoryName = "";
        public ModuleDefMD module;
        public Dictionary<Instruction, int> toFeedS;
        public Dictionary<MethodDef, int> toFeedM;

        public Form1() {
            InitializeComponent();
            toFeedS = new Dictionary<Instruction, int>();
            toFeedM = new Dictionary<MethodDef, int>();
        }

        private void panel1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.Copy;
            } else {
                e.Effect = DragDropEffects.None;
            }
        }


        private void panel1_DragDrop(object sender, DragEventArgs e) {
            try {
                Array array = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (array != null) {
                    string text = array.GetValue(0).ToString();
                    int num = text.LastIndexOf(".", StringComparison.Ordinal);
                    if (num != -1) {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".exe" || text2 == ".dll") {
                            Activate();
                            module = ModuleDefMD.Load(text);
                            richTextBox1.Clear();
                            richTextBox1.AppendText($"[+] File loaded : {module.Name}", Color.Black, true);

                            foreach (var t in module.GetTypes(true)) {
                                richTextBox1.AppendText($"----------------------------\n");
                                richTextBox1.AppendText($"[-]Checking type : {t.Name}\n");
                                foreach (var m in t.Methods(true)) {
                                    if (HasInCompatibleParameters(m))
                                        continue;
                                    if (HasReferencesToFields(m))
                                        continue;
                                    if (HasExternalCall(m))
                                        continue;
                                    //toFeedM.Add(m, 5);
                                    richTextBox1.AppendText($"[+]Compatible method : {m.Name}", Color.Blue, true);
                                }

                                foreach (var f in t.Fields) {
                                    if (!f.FieldType.IsCorLibType)
                                        continue;
                                    richTextBox1.AppendText($"[+]Compatible field : {f.Name}", Color.CadetBlue, true);
                                }
                            }


                            int num2 = text.LastIndexOf("\\", StringComparison.Ordinal);
                            if (num2 != -1) {
                                DirectoryName = text.Remove(num2, text.Length - num2);
                            }
                            if (DirectoryName.Length == 2) {
                                DirectoryName += "\\";
                            }
                        }
                    }
                }
            } catch (Exception ex){
                MessageBox.Show(ex.ToString());
            }
        }

        public bool HasExternalCall(MethodDef m) {
            var instr = m.Body.Instructions;
            foreach (var item in instr) {
                if (item.OpCode == OpCodes.Call && item.Operand is IMethod) {
                    var method = (IMethod)item.Operand;
                    if (!method.DeclaringType.DefinitionAssembly.IsCorLib()) {
                        toFeedM.Add(m, 0);
                        return true;
                    }
                }

                if (item.OpCode == OpCodes.Newobj && item.Operand is IMethod) {
                    var method = (IMethod)item.Operand;
                    if (!method.DeclaringType.DefinitionAssembly.IsCorLib()) {
                        toFeedM.Add(m, 0);
                        return true;
                    }
                }

                if (item.OpCode == OpCodes.Ldtoken && item.Operand is IMethod) {
                    var method = (IMethod)item.Operand;
                    if (!method.DeclaringType.DefinitionAssembly.IsCorLib()) {
                        toFeedM.Add(m, 0);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasReferencesToFields(MethodDef m) {

            var instr = m.Body.Instructions;
            foreach (var item in instr) {
                if (item.OpCode == OpCodes.Ldfld) {
                    toFeedM.Add(m, 0);
                    return true;
                }
                if (item.OpCode == OpCodes.Ldflda) {
                    toFeedM.Add(m, 0);
                    return true;
                }
                if (item.OpCode == OpCodes.Stfld) {
                    toFeedM.Add(m, 0);
                    return true;
                }
                if (item.OpCode == OpCodes.Stsfld) {
                    toFeedM.Add(m, 0);
                    return true;
                }
                if (item.OpCode == OpCodes.Ldsfld) {
                    IField f = (IField)item.Operand;
                    if (!f.FieldSig.Type.DefinitionAssembly.IsCorLib()) {
                        toFeedM.Add(m, 0);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasInCompatibleParameters(MethodDef m) {
            if(m.Parameters.Count == 0) {
                toFeedM.Add(m, 0);
                return true;
            }
            foreach (var p in m.Parameters) {
                if (!p.Type.IsCorLibType) {
                    toFeedM.Add(m, 0);
                    return true;
                }
            }
            if (!m.HasReturnType) {
                toFeedM.Add(m, 0);
                return true;
            }
            return false;
        }

        private void Form1_Load(object sender, EventArgs e) {
            richTextBox1.AppendText("Hello and Welcome to NETLICense.IO Scanner. \n");
            richTextBox1.AppendText("To start, please drag and drop a file \n");
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {

        }

       
    }

    public static class RichTextBoxExtensions {
        public static void AppendText(this RichTextBox box, string text, Color color, bool v) {
            if(v)
                text += Environment.NewLine;

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

}
