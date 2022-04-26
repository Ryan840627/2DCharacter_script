using Ryan;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Player : MonoBehaviour
{
    public static Player instance;

    #region ���}���
    [Header("�K�X"),Tooltip("��n�ഫ�����ɡA�ϥαK�X�ӽT�w�ഫ���y�СA�L�ഫ�������ɡA�|�۰ʽᤩ")]
    public string password;
    [Header("���ʳt��"), Tooltip("�Ψӽվ㨤�Ⲿ�ʳt��"), Range(0, 10)]
    public float speed = 5;
    [Header("���D����"), Range(0, 1000)]
    public float jumpHeight;
    [Header("�Ĩ�t��"), Range(0, 10000)]
    public float dashSpeed = 20;
    [Header("���U�t��")]
    public float speedFall;
    [Header("���D�ɶ�"), Range(0, 1)]
    public double jumpCurrentTime;
    [Header("�����t��"), Range(0, 1)]
    public double attackspeed = 0.5f; //�V�p�V��
    [Header("���˫�L�Įɶ�")]
    public double HurtTime = 5f;
    [Header("�ܤ�����ɶ�")]
    public double DrinkTime = 0.75f;
    [Header("�n�ͦ����l�u����")]
    public GameObject Bullet1;
    public GameObject Bullet2;
    public GameObject Bullet3;
    [Header("�l�u�ͦ�����m")]
    public GameObject BulletPos;
    [Header("�l�u�Ѿl�ƶq")]
    public Text BullteNumber;
    public float facingDirection = -1;
    [Header("�l�u�Ϥ�")]
    public Image BullteImage;
    public Sprite normalBullet;
    public Sprite fireBullet;
    public Sprite icedBullet;
    [Header("�Ĥ��Ѿl�ƶq")]
    public Text healPosionNumberText;
    [Header("�����Ϥ�")]
    public GameObject knife;
    [Header("���a�ʧ@")]
    public Animator PlayerAmimator;
    [Header("�D�����")]
    public Item bullet1;
    public Item bullet2;
    public Item bullet3;
    public Item healPosion;
    [Header("�n�����")]
    public GameObject SoundShootl;
    public GameObject SoundGetHurt;
    public GameObject SoundLand;
    public GameObject SoundDrink;
    [Header("��v�����")]
    public Cinemachine.CinemachineVirtualCamera cam;
    public Vector3 posCameraup;
    public Vector3 posCameradown;
    public Vector3 posCameraorigin;
    [Header("�P�_���b�a���������")]
    public Vector2 v3PlayerGizmosSize = Vector2.one;
    public Vector3 v3PlayerOffect;
    #endregion

    #region �p�H���
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

    #region ��k
    /// <summary>
    /// �e�X�ϫ��Ӥ�K�T�{�j�p
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
    /// ��V�����J
    /// ���U�����|���O�o�� 1 �άO -1 �A�ΨӰϧO���k��V��
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
    /// ���ʤ�k
    /// �ھ� MoveInput() ����V�ʶi�沾�ʡA�ä����H���Ϥ��H�ΰʵe����
    /// </summary>
    /// <param name="speed">���ʳt��</param>
    private void Move(float speed)
    {
        if (!dashing)
        {
            rigid2D.velocity = new Vector2(MoveInput() * speed, rigid2D.velocity.y);
            if (MoveInput() > 0)
            {
                sprRen.flipX = true;
                facingDirection = 1;
                PlayerAmimator.SetBool("�O�_����", true);
            }
            else if (MoveInput() < 0)
            {
                sprRen.flipX = false;
                facingDirection = -1;
                PlayerAmimator.SetBool("�O�_����", true);
            }
            else
            {
                PlayerAmimator.SetBool("�O�_����", false);
            }
        }
    }
    /// <summary>
    /// �P�_�O�_�IĲ�a���A�IĲ�a���~�i�H������D�C
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
    /// �Ĩ��k
    /// 1.�b�a���W�i�H�L���Ĩ�
    /// 2.�b�Ť��u��Ĥ@���A�åB�����쭫�O�v�T
    /// 3.�Ĩ��ھڬO�_��F�Ĩ�ɶ��Ӱ��U
    /// </summary>
    private void Dash()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Dash) && !dashing && !IsDrink)
        {
            jumpingCheck = false;
            dashing = true;
            rigid2D.gravityScale = 0;
            rigid2D.velocity = new Vector2(facingDirection * dashSpeed, 0);
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetTrigger("�]�BĲ�o");
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
    /// ���D��k
    /// �ݭn���P�_�O�_��Ĳ�a���~�i�i����D
    /// </summary>
    private void Jump()
    {
        // ���U X �H���|�V�W����
        if (CheckGround() && Input.GetKeyDown(ChangeInputKey.CIG.Jump) && !IsDrink)
        {
            jumpingCheck = true;
            jumpTimer = jumpCurrentTime;
            rigid2D.velocity = Vector2.up * jumpHeight;
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetBool("�O�_���D", jumpingCheck);
        }
        // ����צ� X �H���|����V�W����
        // �]�w�������צ��ɶ��H������D�̰�����
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
        // ��} X �������U
        else if (Input.GetKeyUp(ChangeInputKey.CIG.Jump) && !IsDrink) jumpingCheck = false;
    }
    /// <summary>
    /// �P�_�H���O�_�q�_�������캢�Űʧ@
    /// </summary>
    private void JumpToFall()
    {
        speedFall = rigid2D.velocity.y;
        if (speedFall <= 1 && CheckGround() == false)
        {
            jumpToFallcheck = true;
            PlayerAmimator.SetBool("�O�_����", jumpToFallcheck);
            jumpingCheck = false;
            PlayerAmimator.SetBool("�O�_���D", jumpingCheck);
        }
    }
    /// <summary>
    /// �H���U�Y�ɤ����ʵe
    /// </summary>
    private void FallingFunction()
    {
        // speedFall < 0 �N��H�����b�U�Y��
        if (speedFall < 0 && jumpToFallcheck == true && CheckGround() == false)
        {
            fallingCheck = true;
            PlayerAmimator.SetBool("�O�_���U", fallingCheck);
            PlayerAmimator.SetBool("�O�_���a", CheckGround());
        }
    }
    /// <summary>
    /// ��Z��������k
    /// �z�L���� knife ����A�èϥΨ�Tag�ӹ�Ǫ��i��ˮ`
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
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetTrigger("����Ĳ�o");
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
    /// �g���欰��k
    /// �z�L���� Bullet ����A�èϥΨ�Tag�ӹ�Ǫ��i��ˮ`�A�ç�s�e��UI���~�ƶq
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
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetTrigger("�g��Ĳ�o");
            Instantiate(SoundShootl, transform.position, Quaternion.identity);
        }
    }
    /// <summary>
    /// �^�_��k
    /// 1.�����b�a���W�~�i�ϥ�
    /// 2.�ϥδ����L�k����(���ʳt�׬�0)
    /// </summary>
    private void Heal()
    {
        if (Input.GetKeyDown(ChangeInputKey.CIG.Heal) && healPosion.itemNumber > 0 && CheckGround() && !IsDrink)
        {
            IsDrink = true;
            Instantiate(SoundDrink, transform);
            GameObject.Find("GM").GetComponent<GM>().HealHP();
            PlayerAmimator.SetTrigger("�ܤ�Ĳ�o");
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
    /// �����g���l�u
    /// ���U�����|�����g���l�u�A�g����k�|�̾�UI�W����ܪ��l�u�i��o�g
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
    /// �L�Įɶ�
    /// ���˫�|���@�q�L�Įɶ��A�����ä��|�A��Ǫ����͸I��
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
    /// �W�U�����k
    /// ����V��W�U�ɡA�i�H�����Y���W�ΤU��V�����A�����L�k����
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
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetBool("�O�_�C�Y", true);
            posCamera = Vector3.Lerp(posCamera, posCameradown, 5 * Time.deltaTime);   //��v���y�� = �t�� (�t�� * �@�V���ɶ� )
            cam.transform.position = posCamera;
        }
        else if (Input.GetKey(ChangeInputKey.CIG.Up))
        {
            speed = 0;
            cam.Follow = null;
            PlayerAmimator.SetBool("�O�_����", false);
            PlayerAmimator.SetBool("�O�_���Y", true);
            posCamera = Vector3.Lerp(posCamera, posCameraup, 5 * Time.deltaTime);
            cam.transform.position = posCamera;
        }
        else if (Input.GetKeyUp(ChangeInputKey.CIG.Up) || Input.GetKeyUp(ChangeInputKey.CIG.Down))
        {
            PlayerAmimator.SetBool("�O�_���Y", false);
            PlayerAmimator.SetBool("�O�_�C�Y", false);
            StartCoroutine(LookBackPlayer());
        }
        else
        {
            posCamera = Vector3.Lerp(posCamera, posCameraorigin, 5 * Time.deltaTime);
            cam.transform.position = posCamera;
        }
        IEnumerator LookBackPlayer()
        {
            PlayerAmimator.SetBool("�O�_����", false);
            yield return new WaitForSeconds(0.5f);
            cam.Follow = gameObject.transform;
            speed = 7;
        }
    }
    /// <summary>
    /// ���a�ɲ��͸��a����
    /// ����k�]�m�b���a�ʵe�̫�~Ĳ�o
    /// </summary>
    public void PlayLandedSound()
    {
        Instantiate(SoundLand, gameObject.transform.position, Quaternion.identity);
    }
    #endregion

    #region �ƥ�

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
            PlayerAmimator.SetBool("�O�_����", jumpToFallcheck);
            PlayerAmimator.SetBool("�O�_���U", fallingCheck);
            PlayerAmimator.SetBool("�O�_���a", CheckGround());
        }
    }
}
#endregion






