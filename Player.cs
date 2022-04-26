using Ryan;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Player : MonoBehaviour
{
    public static Player instance;

    #region 公開欄位
    [Header("密碼"),Tooltip("當要轉換場景時，使用密碼來確定轉換的座標，過轉換場景門時，會自動賦予")]
    public string password;
    [Header("移動速度"), Tooltip("用來調整角色移動速度"), Range(0, 10)]
    public float speed = 5;
    [Header("跳躍高度"), Range(0, 1000)]
    public float jumpHeight;
    [Header("衝刺速度"), Range(0, 10000)]
    public float dashSpeed = 20;
    [Header("落下速度")]
    public float speedFall;
    [Header("跳躍時間"), Range(0, 1)]
    public double jumpCurrentTime;
    [Header("攻擊速度"), Range(0, 1)]
    public double attackspeed = 0.5f; //越小越快
    [Header("受傷後無敵時間")]
    public double HurtTime = 5f;
    [Header("喝水持續時間")]
    public double DrinkTime = 0.75f;
    [Header("要生成的子彈物件")]
    public GameObject Bullet1;
    public GameObject Bullet2;
    public GameObject Bullet3;
    [Header("子彈生成的位置")]
    public GameObject BulletPos;
    [Header("子彈剩餘數量")]
    public Text BullteNumber;
    public float facingDirection = -1;
    [Header("子彈圖片")]
    public Image BullteImage;
    public Sprite normalBullet;
    public Sprite fireBullet;
    public Sprite icedBullet;
    [Header("藥水剩餘數量")]
    public Text healPosionNumberText;
    [Header("攻擊圖片")]
    public GameObject knife;
    [Header("玩家動作")]
    public Animator PlayerAmimator;
    [Header("道具相關")]
    public Item bullet1;
    public Item bullet2;
    public Item bullet3;
    public Item healPosion;
    [Header("聲音資料")]
    public GameObject SoundShootl;
    public GameObject SoundGetHurt;
    public GameObject SoundLand;
    public GameObject SoundDrink;
    [Header("攝影機資料")]
    public Cinemachine.CinemachineVirtualCamera cam;
    public Vector3 posCameraup;
    public Vector3 posCameradown;
    public Vector3 posCameraorigin;
    [Header("判斷站在地面相關資料")]
    public Vector2 v3PlayerGizmosSize = Vector2.one;
    public Vector3 v3PlayerOffect;
    #endregion

    #region 私人欄位
    private bool OnAirFirstDash = false;
    private bool jumpingCheck = false;
    private bool fallingCheck = false;
    private bool jumpToFallcheck = false;
    private double jumpTimer;
    private Rigidbody2D rigid2D;
    private SpriteRenderer sprRen;
    private float dashDuration = 35.0f / 100.0f;
    private float dashCurrentTime;
    private bool dashing = false;
    private double attackCurrentTime;
    private bool attacking = false;
    private double HurtCurrentTime;
    private bool Hurting = false;
    private bool IsDrink = false;
    private double DrinkCurrentTime;
    #endregion

    #region 方法
    /// <summary>
    /// 畫出圖型來方便確認大小
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0.2f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position +
            transform.right * v3PlayerOffect.x +
            transform.up * v3PlayerOffect.y
            , transform.rotation, transform.localScale);
        Gizmos.DrawCube(Vector2.zero, v3PlayerGizmosSize);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Monster")
        {
            Hurting = true;
            Instantiate(SoundGetHurt, gameObject.transform);
            GameObject.Find("GM").GetComponent<GM>().HurtPlayer();
        }
    }
    /// <summary>
    /// 方向按鍵輸入
    /// 按下按鍵後會分別得到 1 或是 -1 ，用來區別左右方向性
    /// </summary>
    private float MoveInput()
    {
        if (Input.GetKey(ChangeInputKey.CIG.Left))
        {
            return -1f;
        }
        else if (Input.GetKey(ChangeInputKey.CIG.Right))
        {
            return 1f;
        }
        else return 0;
    }
    /// <summary>
    /// 移動方法
    /// 根據 MoveInput() 的方向性進行移動，並切換人物圖片以及動畫控制
    /// </summary>
    /// <param name="speed">移動速度</param>
    private void Move(float speed)
    {
        if (!dashing)
        {
            rigid2D.velocity = new Vector2(MoveInput() * speed, rigid2D.velocity.y);
            if (MoveInput() > 0)
            {
                sprRen.flipX = true;
                facingDirection = 1;
                PlayerAmimator.SetBool("是否走路", true);
            }
            else if (MoveInput() < 0)
            {
                sprRen.flipX = false;
                facingDirection = -1;
                PlayerAmimator.SetBool("是否走路", true);
            }
            else
            {
                PlayerAmimator.SetBool("是否走路", false);
            }
        }
    }
    /// <summary>
    /// 判斷是否碰觸地面，碰觸地面才可以執行跳躍。
    /// </summary>
    /// <returns></returns>
    private bool CheckGround()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position +
               transform.right * v3PlayerOffect.x +
               transform.up * v3PlayerOffect.y,
               v3PlayerGizmosSize / 2, 0, 1 << 7);
        return hit;
    }
    /// <summary>
    /// 衝刺方法
    /// 1.在地面上可以無限衝刺
    /// 2.在空中只能衝一次，並且不受到重力影響
    /// 3.衝刺後根據是否到達衝刺時間來停下
    /// </summary>
    private void Dash()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Dash) && !dashing && !IsDrink)
        {
            jumpingCheck = false;
            dashing = true;
            rigid2D.gravityScale = 0;
            rigid2D.velocity = new Vector2(facingDirection * dashSpeed, 0);
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetTrigger("跑步觸發");
            if (!CheckGround() && OnAirFirstDash == false)
            {
                OnAirFirstDash = true;
            }
        }
        if (dashing == true)
        {
            dashCurrentTime += Time.deltaTime;
            if (dashCurrentTime > dashDuration)
            {
                dashing = false;
                dashCurrentTime = 0;
                rigid2D.gravityScale = 5;
            }
        }
        if (CheckGround())
        {
            OnAirFirstDash = false;
        }
    }
    /// <summary>
    /// 跳躍方法
    /// 需要先判斷是否接觸地面才可進行跳躍
    /// </summary>
    private void Jump()
    {
        // 按下 X 人物會向上移動
        if (CheckGround() && Input.GetKeyDown(ChangeInputKey.CIG.Jump) && !IsDrink)
        {
            jumpingCheck = true;
            jumpTimer = jumpCurrentTime;
            rigid2D.velocity = Vector2.up * jumpHeight;
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetBool("是否跳躍", jumpingCheck);
        }
        // 持續案住 X 人物會持續向上移動
        // 設定能夠持續案住的時間以限制跳躍最高高度
        else if (Input.GetKey(ChangeInputKey.CIG.Jump) && jumpingCheck && !IsDrink)
        {
            if (jumpTimer > 0)
            {
                if (dashing)
                {
                    rigid2D.velocity = new Vector2(facingDirection * dashSpeed, 0);
                }
                else
                {
                    rigid2D.velocity = Vector2.up * jumpHeight;
                }
                jumpTimer -= Time.deltaTime;
            }
        }
        // 放開 X 直接落下
        else if (Input.GetKeyUp(ChangeInputKey.CIG.Jump) && !IsDrink) jumpingCheck = false;
    }
    /// <summary>
    /// 判斷人物是否從起跳切換到滯空動作
    /// </summary>
    private void JumpToFall()
    {
        speedFall = rigid2D.velocity.y;
        if (speedFall <= 1 && CheckGround() == false)
        {
            jumpToFallcheck = true;
            PlayerAmimator.SetBool("是否滯空", jumpToFallcheck);
            jumpingCheck = false;
            PlayerAmimator.SetBool("是否跳躍", jumpingCheck);
        }
    }
    /// <summary>
    /// 人物下墜時切換動畫
    /// </summary>
    private void FallingFunction()
    {
        // speedFall < 0 代表人物正在下墜中
        if (speedFall < 0 && jumpToFallcheck == true && CheckGround() == false)
        {
            fallingCheck = true;
            PlayerAmimator.SetBool("是否落下", fallingCheck);
            PlayerAmimator.SetBool("是否落地", CheckGround());
        }
    }
    /// <summary>
    /// 近距離攻擊方法
    /// 透過產生 knife 物件，並使用其Tag來對怪物進行傷害
    /// </summary>
    private void Attack()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Attack) && !attacking && !IsDrink)
        {
            attacking = true;
            if (facingDirection < 0)
            {

                Instantiate(knife, new Vector3(transform.position.x + facingDirection, transform.position.y, transform.position.z), Quaternion.identity);
            }
            else
            {

                Instantiate(knife, new Vector3(transform.position.x + facingDirection, transform.position.y, transform.position.z), Quaternion.identity);
            }
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetTrigger("普攻觸發");
        }
        if (attacking == true)
        {
            attackCurrentTime += Time.deltaTime;
            if (attackCurrentTime > attackspeed)
            {
                attacking = false;
                attackCurrentTime = 0;
            }
        }
    }
    /// <summary>
    /// 射擊行為方法
    /// 透過產生 Bullet 物件，並使用其Tag來對怪物進行傷害，並更新畫面UI物品數量
    /// </summary>
    private void Shoot()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Gun) && !IsDrink)
        {
            if (bullet1.itemNumber > 0 && BullteImage.sprite == normalBullet)
            {
                bullet1.itemNumber--;
                Instantiate(Bullet1, BulletPos.transform.position, BulletPos.transform.rotation);
                BullteNumber.text = "X " + bullet1.itemNumber;
            }
            else if (bullet2.itemNumber > 0 && BullteImage.sprite == fireBullet)
            {
                bullet2.itemNumber--;
                Instantiate(Bullet2, BulletPos.transform.position, BulletPos.transform.rotation);
                BullteNumber.text = "X " + bullet2.itemNumber;
            }
            else if (bullet3.itemNumber > 0 && BullteImage.sprite == icedBullet)
            {
                bullet3.itemNumber--;
                Instantiate(Bullet3, BulletPos.transform.position, BulletPos.transform.rotation);
                BullteNumber.text = "X " + bullet3.itemNumber;
            }
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetTrigger("射擊觸發");
            Instantiate(SoundShootl, transform.position, Quaternion.identity);
        }
    }
    /// <summary>
    /// 回復方法
    /// 1.須站在地面上才可使用
    /// 2.使用期間無法移動(移動速度為0)
    /// </summary>
    private void Heal()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Heal) && healPosion.itemNumber > 0 && CheckGround() && !IsDrink)
        {
            IsDrink = true;
            Instantiate(SoundDrink, transform);
            GameObject.Find("GM").GetComponent<GM>().HealHP();
            PlayerAmimator.SetTrigger("喝水觸發");
            healPosionNumberText.text = "X " + healPosion.itemNumber;
        }
        if (IsDrink == true)
        {
            speed = 0;
            DrinkCurrentTime += Time.deltaTime;
            if (DrinkCurrentTime > DrinkTime)
            {
                IsDrink = false;
                DrinkCurrentTime = 0;
                speed = 7;
            }
        }
    }
    /// <summary>
    /// 切換射擊子彈
    /// 按下按鍵後會切換射擊子彈，射擊方法會依據UI上所顯示的子彈進行發射
    /// </summary>
    private void ChangeBullte()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletLeft) && BullteImage.sprite == normalBullet)
        {
            BullteImage.sprite = fireBullet;
            BullteNumber.text = "X " + bullet2.itemNumber;
        }
        else if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletLeft) && BullteImage.sprite == fireBullet)
        {
            BullteImage.sprite = icedBullet;
            BullteNumber.text = "X " + bullet3.itemNumber;
        }
        else if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletLeft) && BullteImage.sprite == icedBullet)
        {
            BullteImage.sprite = normalBullet;
            BullteNumber.text = "X " + bullet1.itemNumber;
        }
        else if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletRight) && BullteImage.sprite == normalBullet)
        {
            BullteImage.sprite = icedBullet;
            BullteNumber.text = "X " + bullet3.itemNumber;
        }
        else if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletRight) && BullteImage.sprite == icedBullet)
        {
            BullteImage.sprite = fireBullet;
            BullteNumber.text = "X " + bullet2.itemNumber;
        }
        else if (Input.GetKeyDown(ChangeInputKey.CIG.ChangeBulletRight) && BullteImage.sprite == fireBullet)
        {
            BullteImage.sprite = normalBullet;
            BullteNumber.text = "X " + bullet1.itemNumber;
        }
    }
    /// <summary>
    /// 無敵時間
    /// 受傷後會有一段無敵時間，期間並不會再跟怪物產生碰撞
    /// </summary>
    private void NotGetHurt()
    {
        if (Hurting == true)
        {
            Physics2D.IgnoreLayerCollision(6, 8, Hurting);
            HurtCurrentTime += Time.deltaTime;
            if (HurtCurrentTime > HurtTime)
            {
                Hurting = false;
                HurtCurrentTime = 0;
            }
            Physics2D.IgnoreLayerCollision(6, 8, Hurting);
        }
    }
    /// <summary>
    /// 上下眺望方法
    /// 按方向鍵上下時，可以把鏡頭往上或下方向延伸，期間無法移動
    /// </summary>
    private void LookUpAndDown()
    {
        Vector3 posCamera = cam.transform.position;
        if (Input.GetKeyDown(ChangeInputKey.CIG.Up) || Input.GetKeyDown(ChangeInputKey.CIG.Down))
        {
            speed = 0;
            posCameraup = new Vector3(cam.transform.position.x, cam.transform.position.y + 2.5f, cam.transform.position.z);
            posCameradown = new Vector3(cam.transform.position.x, cam.transform.position.y - 2.5f, cam.transform.position.z);
            posCameraorigin = posCamera;
        }
        else if (Input.GetKey(ChangeInputKey.CIG.Down))
        {
            speed = 0;
            cam.Follow = null;
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetBool("是否低頭", true);
            posCamera = Vector3.Lerp(posCamera, posCameradown, 5 * Time.deltaTime);   //攝影機座標 = 差值 (速度 * 一幀的時間 )
            cam.transform.position = posCamera;
        }
        else if (Input.GetKey(ChangeInputKey.CIG.Up))
        {
            speed = 0;
            cam.Follow = null;
            PlayerAmimator.SetBool("是否走路", false);
            PlayerAmimator.SetBool("是否抬頭", true);
            posCamera = Vector3.Lerp(posCamera, posCameraup, 5 * Time.deltaTime);
            cam.transform.position = posCamera;
        }
        else if (Input.GetKeyUp(ChangeInputKey.CIG.Up) || Input.GetKeyUp(ChangeInputKey.CIG.Down))
        {
            PlayerAmimator.SetBool("是否抬頭", false);
            PlayerAmimator.SetBool("是否低頭", false);
            StartCoroutine(LookBackPlayer());
        }
        else
        {
            posCamera = Vector3.Lerp(posCamera, posCameraorigin, 5 * Time.deltaTime);
            cam.transform.position = posCamera;
        }
        IEnumerator LookBackPlayer()
        {
            PlayerAmimator.SetBool("是否走路", false);
            yield return new WaitForSeconds(0.5f);
            cam.Follow = gameObject.transform;
            speed = 7;
        }
    }
    /// <summary>
    /// 落地時產生落地音效
    /// 此方法設置在落地動畫最後才觸發
    /// </summary>
    public void PlayLandedSound()
    {
        Instantiate(SoundLand, gameObject.transform.position, Quaternion.identity);
    }
    #endregion

    #region 事件

    private void Awake()
    {
        if (instance == null) instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnLevelWasLoaded()
    {
        cam = GameObject.Find("CM vcam1").GetComponent<Cinemachine.CinemachineVirtualCamera>();
    }
    private void Start()
    {
        rigid2D = gameObject.GetComponent<Rigidbody2D>();
        sprRen = gameObject.GetComponent<SpriteRenderer>();
        BullteNumber.text = "X " + bullet1.itemNumber;
        healPosionNumberText.text = "X " + healPosion.itemNumber;
        speedFall = rigid2D.velocity.y;
        cam = GameObject.Find("CM vcam1").GetComponent<Cinemachine.CinemachineVirtualCamera>();
    }
    private void Update()
    {
        CheckGround();
        Shoot();
        Heal();
        ChangeBullte();
        Attack();       
        NotGetHurt();
    }
    private void FixedUpdate()
    {
        Jump();
        LookUpAndDown();
        Dash();
        Move(speed);
        JumpToFall();
        FallingFunction();
        if (CheckGround() == true)
        {
            jumpToFallcheck = false;
            fallingCheck = false;
            PlayerAmimator.SetBool("是否滯空", jumpToFallcheck);
            PlayerAmimator.SetBool("是否落下", fallingCheck);
            PlayerAmimator.SetBool("是否落地", CheckGround());
        }
    }
}
#endregion






