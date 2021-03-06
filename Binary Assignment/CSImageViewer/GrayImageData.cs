/**
    \file   GrayImageData.cs
    \brief  Contains GrayImageData class definition.
    \author George J. Grevera, Ph.D., ggrevera@sju.edu

    Copyright (C) 2010, George J. Grevera

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
    USA or from http://www.gnu.org/licenses/gpl.txt.

    This General Public License does not permit incorporating this
    code into proprietary programs.  (So a hypothetical company such
    as GH (Generally Hectic) should NOT incorporate this code into
    their proprietary programs.)
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
//----------------------------------------------------------------------
#pragma warning disable IDE1006

namespace CSImageViewer {

    /** \brief  Class containing the actual pixel data for a gray image
     *  (one value per pixel).
     */
    public class GrayImageData : ImageData {

        /** \brief  This ctor constructs a GrayImageData object from an array
         *  of gray pixel values and the width and height of the image.
         *  mOriginalData will be allocated and set to the pixel values in
         *  unpacked.
         *
         *  \param    unpacked  unpacked array of gray values
         *  \param    w  image width
         *  \param    h  image height
         *  \returns  nothing (ctor)
         */
        public GrayImageData ( int[] unpacked, int w, int h ) {
            Debug.Assert( unpacked != null );
            Debug.Assert( unpacked.Length == w*h );

            mDisplayImage = new Bitmap( w, h, PixelFormat.Format24bppRgb );
            mIsColor = false;
            mImageModified = false;
            mW = w;
            mH = h;
            mOriginalData = new int[ mW * mH ];  // just gray data
            Debug.Assert( mOriginalData != null );
            //determine min and max, and copy unpacked into mOriginalData
            mMin = Int32.MaxValue;
            mMax = Int32.MinValue;
            for (int i = 0; i < w*h; i++) {
                mOriginalData[ i ] = unpacked[ i ];
                if (mOriginalData[ i ] < mMin)    mMin = mOriginalData[ i ];
                if (mOriginalData[ i ] > mMax)    mMax = mOriginalData[ i ];
            }
//            mDisplayData = null;
            unpacked_gray_to_display( unpacked );
        }
        //----------------------------------------------------------------
        /** \brief  Given a bitmap image and a palette (i.e., color lookup
         *  table used to translate scalar image values into other scalar
         *  gray values), this ctor reads the image data, uses the lookup
         *  table to store the pixel data in an array, and creates a
         *  displayable version of the image.  mOriginalData will be
         *  allocated and set to the pixel values in the bitmap.
         *
         *  \param    oldBM  bitmap image used to construct an instance of this class
         *  \param    oldPalette  color lookup table
         *  \param    bpp  bits-per-pixel
         *  \returns  nothing (ctor)
         */
        public GrayImageData ( Bitmap oldBM, Color[] oldPalette, int bpp ) {
            Debug.Assert( oldBM != null );
            Debug.Assert( oldPalette != null );
            Debug.Assert( bpp == 8 || bpp == 4 );
            int w = oldBM.Width;
            int h = oldBM.Height;

            Bitmap bm = new Bitmap( w, h, PixelFormat.Format24bppRgb );
            mDisplayImage = bm;
            mIsColor = false;
            mImageModified = false;
            mW = mDisplayImage.Width;
            mH = mDisplayImage.Height;
            mMin = Int32.MaxValue;
            mMax = Int32.MinValue;
            mOriginalData = new int[ mW * mH ];  // just gray data
            Debug.Assert( mOriginalData != null );
            mDisplayData = null;

            //copy pixel data from oldBM into mOriginalData (and determine min and max)
            Debug.Assert( oldBM.PixelFormat == PixelFormat.Format8bppIndexed );
            BitmapData  bmData = oldBM.LockBits( new Rectangle( 0, 0, w, h ), ImageLockMode.ReadWrite, oldBM.PixelFormat );
            int  stride = bmData.Stride;
            IntPtr  Scan0 = bmData.Scan0;
            if (bpp == 8) {
                unsafe {
                    byte*  p = (byte*) Scan0;
                    int  nOffset = stride - w;
                    int  i = 0;
                    for (int y = 0; y < mH; y++) {
                        for (int x = 0; x < mW; x++) {
                            int  v = p[ 0 ];
                            //make sure that our palette is large enough
                            Debug.Assert( 0 <= v && v < oldPalette.Length );
                            //insure that this is really gray data
                            Debug.Assert( oldPalette[ v ].R == oldPalette[ v ].G && oldPalette[ v ].G == oldPalette[ v ].B );
                            //use the palette to look up the gray value assigned to this image value
                            mOriginalData[ i ] = oldPalette[ v ].R;
                            if (mOriginalData[ i ] < mMin) mMin = mOriginalData[ i ];
                            if (mOriginalData[ i ] > mMax) mMax = mOriginalData[ i ];
                            i++;
                            p++;
                        }  //end for x
                        p += nOffset;
                    }  //end for y
                }  //end unsafe
            }
            else if (bpp == 4) {
                unsafe {
                    byte*  p = (byte*) Scan0;
                    int  nOffset = stride - (w+1)/2;
                    int  i = 0;
                    for (int y = 0; y < mH; y++) {
                        for (int x = 0; x < mW; /*intentionally left blank*/) {

                            //get the high nibble first
                            int  v = p[ 0 ] >> 4;
                            //make sure that our palette is large enough
                            Debug.Assert( 0 <= v && v < oldPalette.Length );
                            //insure that this is really gray data
                            Debug.Assert( oldPalette[ v ].R == oldPalette[ v ].G && oldPalette[ v ].G == oldPalette[ v ].B );
                            //use the palette to look up the gray value assigned to this image value
                            mOriginalData[ i ] = oldPalette[ v ].R;
                            if (mOriginalData[ i ] < mMin)  mMin = mOriginalData[ i ];
                            if (mOriginalData[ i ] > mMax)  mMax = mOriginalData[ i ];
                            i++;
                            x++;
                            if (x >= mW)  break;

                            //now get the next low nibble
                            v = p[ 0 ] & 0x0f;
                            //make sure that our palette is large enough
                            Debug.Assert( 0 <= v && v < oldPalette.Length );
                            //insure that this is really gray data
                            Debug.Assert( oldPalette[ v ].R == oldPalette[ v ].G && oldPalette[ v ].G == oldPalette[ v ].B );
                            //use the palette to look up the gray value assigned to this image value
                            mOriginalData[ i ] = oldPalette[ v ].R;
                            if (mOriginalData[ i ] < mMin)  mMin = mOriginalData[ i ];
                            if (mOriginalData[ i ] > mMax)  mMax = mOriginalData[ i ];
                            i++;
                            x++;

                            p++;
                        }  //end for x
                        p += nOffset;
                    }  //end for y
                }  //end unsafe
            }
            else {
                Debug.Assert( false );  //bad bpp
            }
            oldBM.UnlockBits( bmData );
//            mDisplayData = null;
            unpacked_gray_to_display( mOriginalData );
        }
        //----------------------------------------------------------------
        /** \brief  This function takes an unpacked int array of gray pixel
         *  values (representing an entire image), and copies the values to 
         *  mDisplayData (also representing an entire image).
         *  mDisplayImage is changed to these values as well.
         *  mOriginalData and unpacked remain unchanged.
         *  Note that unpacked must be the same size as the original image 
         *  (mW*mH).
         *
         *  \param    unpacked  unpacked int array of gray values
         *            (all values must be in [0..255]
         *            - no scaling will occur in this function
         *            - all values v will be clamped (not scaled) to 0 <= v <= 255
         *            - copied to mDisplayData
         *  \returns  nothing (void)
         */
        public void unpacked_gray_to_display ( int[] unpacked ) {
            Debug.Assert( unpacked != null );
            Debug.Assert( unpacked.Length == mW*mH );  // gray
            Debug.Assert( !mIsColor );

            //mOriginalData will not be modified.
            // mDisplayData will simply be a (possibly clamped) copy of unpacked.
            if (mDisplayData == null)    mDisplayData = new int[ mW*mH ];  // gray
            Debug.Assert( mDisplayData != null );

            BitmapData  bmData = mDisplayImage.LockBits( new Rectangle( 0, 0, mDisplayImage.Width, mDisplayImage.Height ),
                                                         ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb );
            int         stride = bmData.Stride;
            IntPtr      Scan0  = bmData.Scan0;
            unsafe {
                byte*  p = (byte*) Scan0;
                int    nOffset = stride - mDisplayImage.Width * 3;
                int    nWidth  = mDisplayImage.Width * 3;
                int    i       = 0;
                for (int y = 0; y < mDisplayImage.Height; y++) {
                    for (int x = 0; x < nWidth; x += 3) {
                        int  v = unpacked[ i ];
                        if (v < 0)      v = 0;
                        if (v > 255)    v = 255;
                        //r g b
                        mDisplayData[i] = p[2] = p[1] = p[0] = (byte) v;
                        i++;
                        p += 3;
                    }
                    p += nOffset;
                }
            }
            mDisplayImage.UnlockBits( bmData );
        }
        //----------------------------------------------------------------
        /** \brief    Given a pixel's row and column location, this function
         *            returns the gray pixel value.
         *  \param    row  image row
         *  \param    col  image column
         *  \returns  the gray pixel value at that position.
         */
        public int getGray ( int row, int col ) {
            /** \todo george: speed up indexing */
            int  offset = row * mW + col;
            return offset <= mOriginalData.Length ? mOriginalData[ offset ] : 0;
        }

        public override string GetPixelValue ( int row, int col ) {
            return "GREY:" + getGray( row, col );
        }

    }

}
