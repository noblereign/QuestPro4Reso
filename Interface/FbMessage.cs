using System.Runtime.InteropServices;
using System.Text;

namespace QuestProModule;

public class FbMessage
{
    private const float SranipalNormalizer = 0.75f;
    public readonly float[] Expressions = new float[100];
    

    public void ParseUdp(byte[] data)
    {
        int cursor = 0;
        while (cursor + 8 <= data.Length)
        {
            string str = Encoding.ASCII.GetString(data, cursor, 8);
            cursor += 8;

            switch (str)
            {
                case "EyesQuat":
                    UpdateEyeQuats(data, ref cursor);
                    break;
                case "CombQuat":
                    UpdateCombinedEyeQuats(data, ref cursor);
                    break;
                case "FaceFb\0\0":
                    SetFaceParams(data, ref cursor, FbExpression.Face1_Fb_Max);
                    break;
                case "Face2Fb\0":
                    SetFaceParams(data, ref cursor, FbExpression.Face2_Fb_Max);
                    break;
                default:
                    // Unknown tag or end of data
                    return;
            }
        }

        PrepareUpdate();
    }

    private void UpdateEyeQuats(byte[] data, ref int cursor)
    {
        if (cursor + 32 > data.Length) return;

        Expressions[FbExpression.Left_Rot_X] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Left_Rot_Y] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Left_Rot_Z] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Left_Rot_W] = GetFloat(data, ref cursor);

        Expressions[FbExpression.Right_Rot_X] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Right_Rot_Y] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Right_Rot_Z] = GetFloat(data, ref cursor);
        Expressions[FbExpression.Right_Rot_W] = GetFloat(data, ref cursor);
    }

    private void UpdateCombinedEyeQuats(byte[] data, ref int cursor)
    {
        if (cursor + 16 > data.Length) return;

        float x = GetFloat(data, ref cursor);
        float y = GetFloat(data, ref cursor);
        float z = GetFloat(data, ref cursor);
        float w = GetFloat(data, ref cursor);

        Expressions[FbExpression.Left_Rot_X] = x;
        Expressions[FbExpression.Left_Rot_Y] = y;
        Expressions[FbExpression.Left_Rot_Z] = z;
        Expressions[FbExpression.Left_Rot_W] = w;

        Expressions[FbExpression.Right_Rot_X] = x;
        Expressions[FbExpression.Right_Rot_Y] = y;
        Expressions[FbExpression.Right_Rot_Z] = z;
        Expressions[FbExpression.Right_Rot_W] = w;
    }

    private void SetFaceParams(byte[] data, ref int cursor, int count)
    {
        if (cursor + (count * 4) > data.Length) return;

        for (int i = 0; i < count; i++)
        {
            Expressions[i] = GetFloat(data, ref cursor);
        }
    }

    private float GetFloat(byte[] data, ref int cursor)
    {
        float val = BitConverter.ToSingle(data, cursor);
        cursor += 4;
        return val;
    }

    private static bool FloatNear(float f1, float f2) => Math.Abs(f1 - f2) < 0.0001;

    private void PrepareUpdate()
    {
        // Eye Expressions

        double qX = Expressions[FbExpression.Left_Rot_X];
        double qY = Expressions[FbExpression.Left_Rot_Y];
        double qZ = Expressions[FbExpression.Left_Rot_Z];
        double qW = Expressions[FbExpression.Left_Rot_W];

        double yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
        double pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));
        // Not needed for eye tracking
        // double roll = Math.Atan2(2.0 * (q_x * q_y + q_w * q_z), q_w * q_w + q_x * q_x - q_y * q_y - q_z * q_z); 

        // From radians
        double pitchL = 180.0 / Math.PI * pitch;
        double yawL = 180.0 / Math.PI * yaw;

        qX = Expressions[FbExpression.Right_Rot_X];
        qY = Expressions[FbExpression.Right_Rot_Y];
        qZ = Expressions[FbExpression.Right_Rot_Z];
        qW = Expressions[FbExpression.Right_Rot_W];

        yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
        pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));

        // From radians
        double pitchR = 180.0 / Math.PI * pitch;
        double yawR = 180.0 / Math.PI * yaw;

        // Face Expressions

        // Eyelid edge case, eyes are actually closed now
        if (FloatNear(Expressions[FbExpression.Eyes_Look_Down_L], Expressions[FbExpression.Eyes_Look_Up_L]) &&
            Expressions[FbExpression.Eyes_Closed_L] > 0.25f)
        {
            Expressions[FbExpression.Eyes_Closed_L] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_L] * 3);
        }
        else
        {
            Expressions[FbExpression.Eyes_Closed_L] = 0.9f - Expressions[FbExpression.Eyes_Closed_L] * 3 /
              (1 + Expressions[FbExpression.Eyes_Look_Down_L] * 3);
        }

        // Another eyelid edge case
        if (FloatNear(Expressions[FbExpression.Eyes_Look_Down_R], Expressions[FbExpression.Eyes_Look_Up_R]) &&
            Expressions[FbExpression.Eyes_Closed_R] > 0.25f)
        {
            Expressions[FbExpression.Eyes_Closed_R] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_R] * 3);
        }
        else
        {
            Expressions[FbExpression.Eyes_Closed_R] = 0.9f - Expressions[FbExpression.Eyes_Closed_R] * 3 /
              (1 + Expressions[FbExpression.Eyes_Look_Down_R] * 3);
        }

        //expressions[FBExpression.Lid_Tightener_L = 0.8f-expressions[FBExpression.Eyes_Closed_L]; // Sad: fix combined param instead
        //expressions[FBExpression.Lid_Tightener_R = 0.8f-expressions[FBExpression.Eyes_Closed_R]; // Sad: fix combined param instead

        if (1 - Expressions[FbExpression.Eyes_Closed_L] < Expressions[FbExpression.Lid_Tightener_L])
            Expressions[FbExpression.Lid_Tightener_L] = 1 - Expressions[FbExpression.Eyes_Closed_L] - 0.01f;

        if (1 - Expressions[FbExpression.Eyes_Closed_R] < Expressions[FbExpression.Lid_Tightener_R])
            Expressions[FbExpression.Lid_Tightener_R] = 1 - Expressions[FbExpression.Eyes_Closed_R] - 0.01f;

        //expressions[FBExpression.Lid_Tightener_L = Math.Max(0, expressions[FBExpression.Lid_Tightener_L] - 0.15f);
        //expressions[FBExpression.Lid_Tightener_R = Math.Max(0, expressions[FBExpression.Lid_Tightener_R] - 0.15f);

        Expressions[FbExpression.Upper_Lid_Raiser_L] = Math.Max(0, Expressions[FbExpression.Upper_Lid_Raiser_L] - 0.5f);
        Expressions[FbExpression.Upper_Lid_Raiser_R] = Math.Max(0, Expressions[FbExpression.Upper_Lid_Raiser_R] - 0.5f);

        Expressions[FbExpression.Lid_Tightener_L] = Math.Max(0, Expressions[FbExpression.Lid_Tightener_L] - 0.5f);
        Expressions[FbExpression.Lid_Tightener_R] = Math.Max(0, Expressions[FbExpression.Lid_Tightener_R] - 0.5f);

        Expressions[FbExpression.Inner_Brow_Raiser_L] =
          Math.Min(1, Expressions[FbExpression.Inner_Brow_Raiser_L] * 3f); // * 4;
        Expressions[FbExpression.Brow_Lowerer_L] = Math.Min(1, Expressions[FbExpression.Brow_Lowerer_L] * 3f); // * 4;
        Expressions[FbExpression.Outer_Brow_Raiser_L] =
          Math.Min(1, Expressions[FbExpression.Outer_Brow_Raiser_L] * 3f); // * 4;

        Expressions[FbExpression.Inner_Brow_Raiser_R] =
          Math.Min(1, Expressions[FbExpression.Inner_Brow_Raiser_R] * 3f); // * 4;
        Expressions[FbExpression.Brow_Lowerer_R] = Math.Min(1, Expressions[FbExpression.Brow_Lowerer_R] * 3f); // * 4;
        Expressions[FbExpression.Outer_Brow_Raiser_R] =
          Math.Min(1, Expressions[FbExpression.Outer_Brow_Raiser_R] * 3f); // * 4;

        Expressions[FbExpression.Eyes_Look_Up_L] *= 0.55f;
        Expressions[FbExpression.Eyes_Look_Up_R] *= 0.55f;
        Expressions[FbExpression.Eyes_Look_Down_L] *= 1.5f;
        Expressions[FbExpression.Eyes_Look_Down_R] *= 1.5f;

        Expressions[FbExpression.Eyes_Look_Left_L] *= 0.85f;
        Expressions[FbExpression.Eyes_Look_Right_L] *= 0.85f;
        Expressions[FbExpression.Eyes_Look_Left_R] *= 0.85f;
        Expressions[FbExpression.Eyes_Look_Right_R] *= 0.85f;

        // Hack: turn rots to looks
        // Pitch = 29(left)-- > -29(right)
        // Yaw = -27(down)-- > 27(up)

        if (pitchL > 0)
        {
            Expressions[FbExpression.Eyes_Look_Left_L] = Math.Min(1, (float)(pitchL / 29.0)) * SranipalNormalizer;
            Expressions[FbExpression.Eyes_Look_Right_L] = 0;
        }
        else
        {
            Expressions[FbExpression.Eyes_Look_Left_L] = 0;
            Expressions[FbExpression.Eyes_Look_Right_L] = Math.Min(1, (float)(-pitchL / 29.0)) * SranipalNormalizer;
        }

        if (yawL > 0)
        {
            Expressions[FbExpression.Eyes_Look_Up_L] = Math.Min(1, (float)(yawL / 27.0)) * SranipalNormalizer;
            Expressions[FbExpression.Eyes_Look_Down_L] = 0;
        }
        else
        {
            Expressions[FbExpression.Eyes_Look_Up_L] = 0;
            Expressions[FbExpression.Eyes_Look_Down_L] = Math.Min(1, (float)(-yawL / 27.0)) * SranipalNormalizer;
        }


        if (pitchR > 0)
        {
            Expressions[FbExpression.Eyes_Look_Left_R] = Math.Min(1, (float)(pitchR / 29.0)) * SranipalNormalizer;
            Expressions[FbExpression.Eyes_Look_Right_R] = 0;
        }
        else
        {
            Expressions[FbExpression.Eyes_Look_Left_R] = 0;
            Expressions[FbExpression.Eyes_Look_Right_R] = Math.Min(1, (float)(-pitchR / 29.0)) * SranipalNormalizer;
        }

        if (yawR > 0)
        {
            Expressions[FbExpression.Eyes_Look_Up_R] = Math.Min(1, (float)(yawR / 27.0)) * SranipalNormalizer;
            Expressions[FbExpression.Eyes_Look_Down_R] = 0;
        }
        else
        {
            Expressions[FbExpression.Eyes_Look_Up_R] = 0;
            Expressions[FbExpression.Eyes_Look_Down_R] = Math.Min(1, (float)(-yawR / 27.0)) * SranipalNormalizer;
        }
    }
}
