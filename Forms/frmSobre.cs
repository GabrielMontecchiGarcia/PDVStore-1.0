using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    partial class frmSobre : Form
    {
        // Construtor da tela "Sobre": preenche os rótulos com dados do assembly.
        // O QUE FAZ: chama InitializeComponent e preenche título, produto, versão,
        // copyright, empresa e a caixa de descrição usando as propriedades de leitura
        // dos metadados do executável.
        // POR QUE EXISTE: mostra ao usuário as informações da versão em execução — útil
        // para suporte e identificação do build.
        // DEPENDÊNCIAS: AssemblyTitle, AssemblyProduct, AssemblyVersion etc.
        public frmSobre()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle);
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
            this.textBoxDescription.Text = AssemblyDescription;
        }

        #region Assembly Attribute Accessors

        // Lê o atributo AssemblyTitle do executável (nome exibido na barra de título).
        // POR QUE EXISTE: o valor vem do arquivo de projeto/AssemblyInfo, não é digitado
        // à mão, garantindo que a tela "Sobre" reflita o título oficial do produto.
        // QUEM CHAMA: o construtor de frmSobre.
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        // Lê a versão compilada do executável (ex.: "1.0.0.0").
        // POR QUE EXISTE: mostra qual versão do sistema está instalada, essencial para
        // conferir atualizações e reportar bugs.
        // QUEM CHAMA: o construtor de frmSobre (labelVersion).
        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        // Lê a descrição da aplicação declarada no assembly.
        // POR QUE EXISTE: alimenta a caixa de texto de descrição da janela "Sobre",
        // exibindo uma breve apresentação do sistema ao usuário.
        // QUEM CHAMA: o construtor de frmSobre (textBoxDescription).
        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        // Lê o nome do produto registrado nos metadados do assembly.
        // POR QUE EXISTE: centraliza o nome comercial em um único lugar (AssemblyInfo)
        // e a tela "Sobre" apenas o reflete, evitando texto duplicado no código.
        // QUEM CHAMA: o construtor de frmSobre (labelProductName).
        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        // Lê o aviso de copyright registrado no assembly.
        // POR QUE EXISTE: exibe legalmente o titular dos direitos sobre o software na
        // tela "Sobre", usando a informação oficial do metadado.
        // QUEM CHAMA: o construtor de frmSobre (labelCopyright).
        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        // Lê o nome da empresa (Company) declarado no assembly.
        // POR QUE EXISTE: identifica o fornecedor/instituição responsável pelo sistema
        // na tela "Sobre", sem hardcode no formulário.
        // QUEM CHAMA: o construtor de frmSobre (labelCompanyName).
        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion
    }
}
