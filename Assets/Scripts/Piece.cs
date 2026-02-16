using DG.Tweening;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public int x;
    public int y;
    public Board board;

    public enum type 
    {
        elephant,
        giraffe,
        hippo,
        monkey,
        panda,
        parrot,
        penguin,
        pig,
        rabbit,
        snake,
    };

    public type pieceType;

    public void Setup(int x_, int y_, Board board_) {
        x = x_;
        y = y_;
        board = board_;

        transform.localScale = Vector3.one * 0.35f;
        transform.DOScale(Vector3.one, 0.35f);
    }


    public void Move(int desX, int desY)
    {
        transform.DOMove(new Vector3(desX, desY, -5), 0.25f).SetEase(Ease.InOutCubic).onComplete = () =>
        {
            x = desX;
            y = desY;
        };
    }

    public void Remove(bool animated)
    {
        if (animated)
        {
            transform.DOScale(Vector3.one * 1.2f, 0.085f).onComplete = () =>
            {
                transform.DOScale(Vector3.zero, 0.1f).onComplete = () =>
                {
                    Destroy(gameObject);
                };
            };
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Move Test")]
    public void MoveTest()
    {
        Move(0, 0);
    }
}
