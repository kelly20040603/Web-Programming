using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

namespace Tennis
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // 公用變數
        Socket T; // 通訊物件
        Thread Th; // 網路監聽執行緒
        string User; // 使用者名稱
        int mdx; // 拖曳球拍起點
        int oX = 0; // 球拍位置
        private bool gameOver = false; // 標記遊戲是否結束

        int playerScore = 0; // 玩家分數
        int opponentScore = 0; // 對手分數
        int speedIncreaseCount = 0; // 用來增加球速的計數器

        // 監聽 Server 訊息
        private void Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint; //Server 的 EndPoint
            byte[] B = new byte[1023]; //接收用的 Byte 陣列
            int inLen = 0; //接收的位元組數目
            string Msg; //接收到的完整訊息
            string St; //命令碼
            string Str; //訊息內容(不含命令碼)

            while (true)//無限次監聽迴圈
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP);//收聽資訊並取得位元組數
                }
                catch (Exception)
                {
                    T.Close();//關閉通訊器
                    ListBox1.Items.Clear();//清除線上名單
                    MessageBox.Show("伺服器斷線了！");//顯示斷線
                    Button1.Enabled = true;//連線按鍵恢復可用
                    Th.Abort();//刪除執行緒
                }

                Msg = Encoding.Default.GetString(B, 0, inLen); //解讀完整訊息
                St = Msg.Substring(0, 1); //取出命令碼 (第一個字)
                Str = Msg.Substring(1); //取出命令碼之後的訊息
                                        //
                switch (St)//依命令碼執行功能
                {
                    case "L"://接收線上名單
                        ListBox1.Items.Clear(); //清除名單
                        string[] M = Str.Split(','); //拆解名單成陣列
                        for (int i = 0; i < M.Length; i++)
                        {
                            ListBox1.Items.Add(M[i]); //逐一加入名單
                        }
                        break;
                    case "7"://對手球拍移動訊息
                        H2.Left = G.Width - int.Parse(Str) - H2.Width; //鏡射之後的位置
                        break;
                    case "8"://球的位置同步訊息
                        string[] C = Str.Split(',');//切割訊號
                        Q.Left = G.Width - int.Parse(C[0]) - Q.Width; //左右鏡射位置
                        Q.Top = G.Height - Q.Height - int.Parse(C[1]); //上下鏡射位置
                        break;
                    case "5":
                        DialogResult result = MessageBox.Show("是否重玩遊戲(得" + Str + "分結束)?", "重玩訊息", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            comboBox1.Text = Str;
                            comboBox1.Enabled = false;
                            Send("P" + "Y" + "|" + ListBox1.SelectedItem);
                        }
                        else
                        {
                            Send("P" + "N" + "|" + ListBox1.SelectedItem);
                        }
                        break;
                    case "P":
                        if (Str == "Y")
                        {
                            MessageBox.Show(ListBox1.SelectedItem.ToString() + "接受你的邀請，可以開始重玩遊戲");
                            GO.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show("抱歉" + ListBox1.SelectedItem.ToString() + "拒絕你的邀請");
                        }
                        break;
                    case "D":
                        TextBox4.Text = Str;
                        Button1.Enabled = true;
                        button3.Enabled = false;
                        T.Close();
                        Th.Abort();
                        break;
                    case "I":
                        string[] F = Str.Split(',');
                        DialogResult res = MessageBox.Show(F[0] + "邀請玩遊戲(得" + F[1] + "分結束)，是否接受?", "邀請訊息", MessageBoxButtons.YesNo);
                        if (res == DialogResult.Yes)
                        {
                            int i = ListBox1.Items.IndexOf(F[0]);
                            ListBox1.SetSelected(i, true);
                            ListBox1.Enabled = false;
                            comboBox1.Text = F[1];
                            comboBox1.Enabled = false;
                            button3.Enabled = false;
                            button2.Enabled = true;
                            GO.Enabled = true;
                            Send("R" + "Y" + "|" + F[0]);
                        }
                        else
                        {
                            Send("R" + "N" + "|" + F[0]);
                        }
                        break;
                    case "R":
                        if (Str == "Y")
                        {
                            MessageBox.Show(ListBox1.SelectedItem.ToString() + "接受你的邀請，可以開始遊戲");
                            ListBox1.Enabled = false;
                            comboBox1.Enabled = false;
                            button3.Enabled = false;
                            button2.Enabled = true;
                            GO.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show("抱歉" + ListBox1.SelectedItem.ToString() + "拒絕你的邀請");
                        }
                        break;
                    case "3":
                        TextBox4.Text = "使用者名稱重複，請重新輸入";
                        Button1.Enabled = true;
                        button2.Enabled = false;
                        button3.Enabled = false;
                        T.Close();
                        Th.Abort();
                        break;
                    case "S": // 接收對方的分數資訊
                        string[] scores = Str.Split(',');
                        int opponentPlayerScore = int.Parse(scores[0]);
                        int opponentOpponentScore = int.Parse(scores[1]);

                        // 更新對手分數顯示或做其他處理
                        LabelOpponentScore.Text = "對手得分: " + opponentPlayerScore.ToString();
                        LabelPlayerScore.Text = "我方得分: " + opponentOpponentScore.ToString();

                        int scoreLimit = int.Parse(comboBox1.Text);
                        // 檢查勝負
                        if (opponentOpponentScore == scoreLimit)
                        {
                            MessageBox.Show(opponentOpponentScore + "比" + opponentPlayerScore + "我方獲勝!");
                            ResetBall();
                        }
                        else if (opponentPlayerScore == scoreLimit)
                        {
                            MessageBox.Show(opponentOpponentScore + "比" + opponentPlayerScore + "對手獲勝!");
                            ResetBall();
                        }
                        break;
                }
            }
        }

        // 送出訊息
        private void Send(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str); //翻譯文字成Byte陣列
            T.Send(B, 0, B.Length, SocketFlags.None); //傳送訊息給伺服器
        }
        private string MyIP()
        {
            string hn = Dns.GetHostName(); // 獲取主機名稱
            IPAddress[] ip = Dns.GetHostEntry(hn).AddressList; // 獲取所有 IP 地址
            foreach (IPAddress it in ip)
            {
                if (it.AddressFamily == AddressFamily.InterNetwork) // 過濾 IPv4 地址
                {
                    return it.ToString(); // 返回第一個 IPv4 地址
                }
            }
            return ""; // 如果沒有找到，返回空字符串
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text += MyIP(); // 在窗體標題中顯示本地 IP 地址
            button2.Enabled = false; // 禁用「開始遊戲」按鈕
            button3.Enabled = false; // 禁用「邀請玩家」按鈕
            GO.Enabled = false; // 禁用「開始遊戲」按鈕
        }

        // 關閉視窗離線
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Button1.Enabled == false)//如果已經上線
            {
                Send("9" + User);  // 在關閉表單前，通知伺服器用戶已離線。
                T.Close();        // 關閉套接字連線。
            }
        }

        //開始拖曳球拍
        private void H1_MouseDown(object sender, MouseEventArgs e)
        {
            mdx = e.X; //拖曳起點
        }
        //拖曳球拍中
        private void H1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                int X = H1.Left + e.X - mdx; //試算拖曳終點位置
                if (X < 0) X = 0; //不能超出左邊界
                if (X > G.Width - H1.Width) X = G.Width - H1.Width; //不能超出右邊界
                H1.Left = X; //設定X為球拍座標
                if (ListBox1.SelectedIndex >= 0)//有選取遊戲對手，上線遊戲中
                {
                    if (oX != H1.Left)//球拍已移動
                    {
                        Send("7" + H1.Left.ToString() + "|" + ListBox1.SelectedItem); //傳送球拍位置訊息
                        oX = H1.Left;//紀錄球拍新位置
                    }
                }
            }
        }

        // 控制球移動的程式
        private void Timer1_Tick(object sender, EventArgs e)
        {
            Point V = (Point)Q.Tag; //取出速度
            Q.Left += V.X; //移動X
            Q.Top += V.Y;  //移動Y
            chkHit(Q, G, true);   //檢查與處理球與內牆的碰撞
            chkHit(Q, H1, false); //檢查與處理球與自己球拍的碰撞
            chkHit(Q, H2, false); //檢查與處理球與對手球拍的碰撞

            // 如果球進入對方區域得分
            if (Q.Top > G.Height)
            {
                opponentScore++;
                UpdateScore(); // 更新分數顯示
                ResetBall();   // 重置球的位置
            }
            else if (Q.Top <= 0)
            {
                playerScore++;
                UpdateScore(); // 更新分數顯示
                ResetBall();   // 重置球的位置
            }

            // 逐漸增加球速
            if (speedIncreaseCount >= 2) // 每2次增加球速
            {
                IncreaseSpeed(); // 增加球速
                speedIncreaseCount = 0; // 重置計數器
            }
            else
            {
                speedIncreaseCount++; // 增加計數器
            }

            // 強制更新畫面
            this.Invalidate(); // 重新繪製畫面

            if (ListBox1.SelectedIndex >= 0) //有選取遊戲對手，上線遊戲中
            { //傳送球的位置
                Send("8" + Q.Left.ToString() + "," + Q.Top.ToString() + "|" + ListBox1.SelectedItem);
            }
        }

        // 碰撞檢查程式
        private bool chkHit(Label B, object C, bool inside)
        {
            Point V = (Point)B.Tag;//自物件的Tag屬性取出速度值(V.x, V.y)
            if (inside)//球與牆壁的碰撞偵測
            {
                Panel p = (Panel)C;
                if (B.Right > p.Width)//右牆碰撞
                {
                    V.X = -Math.Abs(V.X);
                    B.Tag = V;
                    return true;
                }
                if (B.Left < 0)//左牆碰撞
                {
                    V.X = Math.Abs(V.X);
                    B.Tag = V;
                    return true;
                }
                if (B.Bottom > p.Height)//地板碰撞
                {
                    V.X = -Math.Abs(V.X);
                    B.Tag = V;
                    return true;
                }
                if (B.Top < 0)//屋頂碰撞
                {
                    V.X = Math.Abs(V.X);
                    B.Tag = V;
                    return true;
                }
                return false;//未發生碰撞
            }
            else//求羽球拍的碰撞偵測
            {
                Label k = (Label)C;
                if (B.Right < k.Left) return false;//球在物件之左確定未碰撞
                if (B.Left > k.Right) return false;//球在物件之右確定未碰撞
                if (B.Bottom < k.Top) return false;//球在物件之上確定未碰撞
                if (B.Top > k.Bottom) return false;//球在物件之下確定未碰撞
                //    目標左側碰撞
                if (B.Right >= k.Left && (B.Right - k.Left) <= Math.Abs(V.X)) V.X = -Math.Abs(V.X);
                //    目標右側碰撞
                if (B.Left <= k.Right && (k.Right - B.Left) <= Math.Abs(V.X)) V.X = Math.Abs(V.X);
                //    目標底部碰撞
                if (B.Top <= k.Bottom && (k.Bottom - B.Top) <= Math.Abs(V.Y)) V.Y = Math.Abs(V.Y);
                //    目標頂部碰撞
                if (B.Bottom >= k.Top && (B.Bottom - k.Top) <= Math.Abs(V.Y)) V.Y = -Math.Abs(V.Y);
                B.Tag = V;//紀錄球速度(方向)
                return true;//回應有發生碰撞
            }
        }

        // 更新分數顯示並分享給對手
        private void UpdateScore()
        {
            LabelPlayerScore.Text = "我方得分: " + playerScore.ToString();
            LabelOpponentScore.Text = "對手得分: " + opponentScore.ToString();

            int scoreLimit = int.Parse(comboBox1.Text);

            if (playerScore == scoreLimit && !gameOver)
            {
                MessageBox.Show(playerScore + "比" + opponentScore + "我方獲勝!");
                gameOver = true; // 標記遊戲結束
                ResetGame();
            }
            else if (opponentScore == scoreLimit && !gameOver)
            {
                MessageBox.Show(playerScore + "比" + opponentScore + "對手獲勝!");
                gameOver = true; // 標記遊戲結束
                ResetGame();
            }

            // 發送分數給對手
            if (ListBox1.SelectedIndex >= 0)
            {
                string scoreData = "S" + playerScore.ToString() + "," + opponentScore.ToString();
                Send(scoreData + "|" + ListBox1.SelectedItem); // 發送分數資訊
            }

            // 檢查勝負
            if (playerScore == scoreLimit && !gameOver)
            {
                MessageBox.Show(playerScore + "比" + opponentScore + "我方獲勝!");
                gameOver = true; // 標記遊戲結束
                ResetGame();
            }
            else if (opponentScore == scoreLimit && !gameOver)
            {
                MessageBox.Show(playerScore + "比" + opponentScore + "對手獲勝!");
                gameOver = true; // 標記遊戲結束
                ResetGame();
            }
        }


        // 重置遊戲
        private void ResetGame()
        {
            StopBall();
            gameOver = false; // 重置遊戲結束標記           
            playerScore = 0; // 重置玩家分數
            opponentScore = 0; // 重置對手分數
            LabelPlayerScore.Text = "我方得分: 0";
            LabelOpponentScore.Text = "對手得分: 0";
        }
        private void StopBall()
        {
            Q.Left = G.Width / 2 - Q.Width / 2;
            Q.Top = G.Height / 2 - Q.Height / 2;
            Timer1.Stop(); // 停止計時器
        }

        // 啟動遊戲
        private void GO_Click(object sender, EventArgs e)
        {
            Q.Tag = new Point(5, -5); // 預設速度(往右上)
            Timer1.Start(); // 開始移動
        }

        // 連線伺服器按鈕點擊事件
        private void button1_Click(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false; //忽略跨執行緒操作的錯誤
            User = TextBox3.Text; // 獲取用戶名稱
            string IP = TextBox1.Text; // 獲取伺服器 IP 地址
            int Port = int.Parse(TextBox2.Text); // 獲取伺服器Port
            try
            {
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port); // 建立伺服器端點資訊
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //建立TCP通訊物件
                T.Connect(EP); //連上Server的EP端點(類似撥號連線)
                Th = new Thread(Listen); //建立監聽執行緒
                Th.IsBackground = true; //設定為背景執行緒
                Th.Start(); //開始監聽
                TextBox4.Text = "已連線伺服器!" + "\r\n"; // 顯示連線成功訊息
                button2.Enabled = true;
                Send("0" + User); // 發送用戶名稱到伺服器
                Button1.Enabled = false; // 禁用「連線伺服器」按鈕
                button3.Enabled = true; // 啟用「邀請玩家」按鈕
                button2.Enabled = false; // 禁用「重新開始」按鈕
                GO.Enabled = false; // 禁用「開始遊戲」按鈕
            }
            catch
            {
                TextBox4.Text = "無法連上伺服器！" + "\r\n";// 顯示連線失敗訊息
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (ListBox1.SelectedIndex >= 0)
            {
                Send("5" + comboBox1.Text + "|" + ListBox1.SelectedItem);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (ListBox1.SelectedIndex != -1)
            {
                if (ListBox1.SelectedItem.ToString() != User)
                {
                    Send("I" + User + "," + comboBox1.Text + "|" + ListBox1.SelectedItem);
                }
                else
                {
                    MessageBox.Show("不可以邀請自己!");
                }
            }
            else
            {
                MessageBox.Show("沒有選取邀請的對象!");
            }
        }

        // 重置球的位置
        private void ResetBall()
        {
            Q.Left = G.Width / 2 - Q.Width / 2;
            Q.Top = G.Height / 2 - Q.Height / 2;
            Q.Tag = new Point(5, -5); // 重置球的速度
        }

        // 增加球速
        private void IncreaseSpeed()
        {
            Point currentSpeed = (Point)Q.Tag;
            currentSpeed.X = (int)(currentSpeed.X * 1.1); // 逐漸增加X軸速度
            currentSpeed.Y = (int)(currentSpeed.Y * 1.1); // 逐漸增加Y軸速度
            Q.Tag = currentSpeed;
        }
    }
}
